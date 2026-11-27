import csv
import io
import json
import os
import secrets
import unittest
from concurrent.futures import ThreadPoolExecutor
from datetime import date, datetime, timedelta, timezone
from decimal import Decimal
from threading import Barrier
from time import monotonic, sleep
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen
from zoneinfo import ZoneInfo

BASE_URL = os.getenv("FINTRACK_TEST_URL", "http://127.0.0.1:8080").rstrip("/")


def request(method, path, body=None, token=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    data = json.dumps(body).encode() if body is not None else None
    http_request = Request(BASE_URL + path, data=data, headers=headers, method=method)
    try:
        response = urlopen(http_request, timeout=10)
    except HTTPError as error:
        response = error
    with response:
        content = response.read()
        return response.status, json.loads(content, parse_float=Decimal) if content else None


class FinancialIntegrationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        deadline = monotonic() + 60
        while monotonic() < deadline:
            try:
                status, swagger = request("GET", "/swagger/v1/swagger.json")
                if status == 200 and "/api/Transfers" in swagger["paths"]:
                    return
            except (URLError, ConnectionError, TimeoutError):
                pass
            sleep(1)
        raise RuntimeError("The FinTrack API with transfers did not become ready.")

    def call(self, method, path, expected, body=None, token=None):
        status, result = request(method, f"/api/{path}", body, token)
        self.assertEqual(status, expected, f"{method} /api/{path}: {result}")
        return result

    def register(self):
        credentials = {
            "email": f"fintrack-ci-{secrets.token_hex(8)}@example.com",
            "password": secrets.token_urlsafe(24),
        }
        self.call("POST", "auth/register", 200, {"name": "Integration Test", **credentials})
        self.last_credentials = credentials
        return self.call("POST", "auth/login", 200, credentials)["token"]

    def create_account(self, token, name, balance):
        body = {"name": name, "type": 1, "initialBalance": balance}
        return self.call("POST", "accounts", 201, body, token)["id"]

    def setUp(self):
        self.token = self.register()
        self.login_credentials = self.last_credentials
        self.origin = self.create_account(self.token, "Origin", 1000)
        self.destination = self.create_account(self.token, "Destination", 200)
        now = datetime.now(timezone.utc)
        self.monthly_path = f"summaries/monthly?month={now.month}&year={now.year}"
        self.transfer = {
            "description": "Integration test transfer",
            "amount": 125.5,
            "date": now.isoformat().replace("+00:00", "Z"),
            "fromAccountId": self.origin,
            "toAccountId": self.destination,
        }

    def assert_balances(self, origin, destination):
        accounts = self.call("GET", "accounts", 200, token=self.token)
        self.assertEqual(
            {account["id"]: account["currentBalance"] for account in accounts},
            {self.origin: origin, self.destination: destination},
        )
        total = self.call("GET", "summaries/overall-balance", 200, token=self.token)
        self.assertEqual(total["totalBalance"], 1200)

    def recurrence_request(self, automatic, amount, start, transaction_type=1):
        category = self.call(
            "POST", "categories", 201,
            {"name": "Recurring category", "type": transaction_type}, self.token,
        )
        return {
            "description": "Recurring test",
            "accountId": self.origin,
            "categoryId": category["id"],
            "startsOn": start,
            "type": transaction_type,
            "automatic": automatic,
            "amount": amount,
        }

    def agenda(self, token=None):
        return self.call("GET", "recurrences/agenda", 200, token=token or self.token)

    def wait_for(self, query, condition):
        deadline = monotonic() + 25
        while monotonic() < deadline:
            value = query()
            if condition(value):
                return value
            sleep(0.25)
        self.fail("The recurring worker did not produce the expected result.")

    def test_automatic_recurrence_recovers_missed_months_and_projects_the_next(self):
        today = date.fromisoformat(self.agenda()["today"])
        previous_month = today.replace(day=1) - timedelta(days=1)
        start = previous_month.replace(day=1).isoformat()
        body = self.recurrence_request(True, 100, start)
        self.call("POST", "recurrences", 400, {**body, "amount": None}, self.token)
        rule = self.call("POST", "recurrences", 201, body, self.token)
        transactions = self.wait_for(
            lambda: self.call("GET", "transactions", 200, token=self.token),
            lambda rows: len(rows) >= 2,
        )
        self.assertCountEqual([item["date"][:10] for item in transactions],
                              [start, today.replace(day=1).isoformat()])
        self.assertEqual(self.call("GET", f"accounts/{self.origin}", 200,
                                   token=self.token)["currentBalance"], 1200)
        agenda = self.agenda()
        self.assertTrue(all(item["date"] > today.isoformat() for item in agenda["items"]))
        projection = next(item for item in agenda["accounts"] if item["id"] == self.origin)
        expected = 1200 + sum(item["amount"] for item in agenda["items"])
        self.assertEqual(projection["projectedBalance"], expected)
        self.call("DELETE", f"recurrences/{rule['id']}", 204, token=self.token)
        self.assertEqual(len(self.call("GET", "transactions", 200, token=self.token)), 2)

    def test_manual_recurrence_requires_the_actual_value_and_posts_only_once(self):
        today = self.agenda()["today"]
        body = self.recurrence_request(False, None, today, transaction_type=2)
        rule = self.call("POST", "recurrences", 201, body, self.token)
        path = f"recurrences/{rule['id']}/occurrences/{today}/confirm"
        upcoming = self.wait_for(self.agenda, lambda agenda: any(item["pending"] for item in agenda["items"]))
        account = next(item for item in upcoming["accounts"] if item["id"] == self.origin)
        self.assertEqual(account["currentBalance"], 1000)
        self.assertTrue(account["hasUnknownAmounts"])
        self.call("POST", path, 400, {"amount": None}, self.token)
        self.call("POST", path, 400, {"amount": 0.001}, self.token)
        other = self.register()
        self.assertEqual(self.agenda(other)["items"], [])
        self.call("POST", path, 400, {"amount": 75.5}, other)
        self.call("PATCH", f"recurrences/{rule['id']}", 404, {"paused": True}, other)

        def confirm(_):
            return request("POST", f"/api/{path}", {"amount": 75.5}, self.token)[0]

        with ThreadPoolExecutor(max_workers=2) as pool:
            statuses = list(pool.map(confirm, range(2)))
        self.assertIn(204, statuses)
        self.assertTrue(all(status in (204, 409) for status in statuses))
        self.call("POST", path, 204, {"amount": 75.5}, self.token)
        self.assertEqual(len(self.call("GET", "transactions", 200, token=self.token)), 1)
        self.assertEqual(self.call("GET", f"accounts/{self.origin}", 200,
                                   token=self.token)["currentBalance"], Decimal("924.50"))
        self.assertFalse(any(item["pending"] for item in self.agenda()["items"]))

    def test_pause_and_resume_update_the_agenda_without_posting_future_transactions(self):
        start = (date.fromisoformat(self.agenda()["today"]) + timedelta(days=2)).isoformat()
        body = self.recurrence_request(True, 250, start)
        rule = self.call("POST", "recurrences", 201, body, self.token)
        path = f"recurrences/{rule['id']}"
        self.assertIn(start, [item["date"] for item in self.agenda()["items"]])
        self.call("PATCH", path, 204, {"paused": True}, self.token)
        self.assertEqual(self.agenda()["items"], [])
        self.call("DELETE", f"categories/{body['categoryId']}", 409, token=self.token)
        self.call("PATCH", path, 204, {"paused": False}, self.token)
        self.assertIn(start, [item["date"] for item in self.agenda()["items"]])
        self.assertEqual(self.call("GET", "transactions", 200, token=self.token), [])

    def test_refresh_session_rotates_the_cookie_and_logout_revokes_it(self):
        def session_request(path, body=None, cookie=None, send_header=True):
            headers = {"Content-Type": "application/json"}
            if send_header:
                headers["X-FinTrack-Session"] = "1"
            if cookie:
                headers["Cookie"] = cookie
            data = json.dumps(body).encode() if body is not None else None
            req = Request(f"{BASE_URL}/api/auth/{path}", data=data, headers=headers, method="POST")
            try:
                response = urlopen(req, timeout=10)
            except HTTPError as error:
                response = error
            with response:
                return response.status, response.headers.get("Set-Cookie"), response.read()

        status, original, _ = session_request("login", {
            "email": self.login_credentials["email"], "password": self.login_credentials["password"],
        })
        self.assertEqual(status, 200)
        self.assertIn("httponly", original.lower())
        self.assertIn("samesite=strict", original.lower())
        original = original.split(";", 1)[0]
        self.assertEqual(session_request("refresh", cookie=original, send_header=False)[0], 400)
        status, rotated, body = session_request("refresh", cookie=original)
        self.assertEqual(status, 200)
        rotated = rotated.split(";", 1)[0]
        self.assertNotEqual(rotated, original)
        self.assertEqual(session_request("refresh", cookie=original)[0], 401)
        self.call("GET", "accounts", 200, token=json.loads(body)["token"])
        self.assertEqual(session_request("logout", cookie=rotated)[0], 204)
        self.assertEqual(session_request("refresh", cookie=rotated)[0], 401)

    def test_privacy_requires_current_password_and_only_removes_the_signed_in_profile(self):
        credentials = self.login_credentials
        privacy = {"password": credentials["password"]}
        self.call("POST", "auth/privacy/export", 401, privacy)
        self.call("POST", "auth/privacy/export", 403, {"password": "wrong"}, self.token)
        self.call("POST", "auth/privacy/delete", 403, {"password": "wrong"}, self.token)

        other = self.register()
        other_id = self.create_account(other, "Other profile", 20)
        today = datetime.now(ZoneInfo("America/Sao_Paulo")).date()
        rule = self.recurrence_request(False, None, str(today), transaction_type=2)
        self.call("POST", "recurrences", 201, rule, self.token)
        self.call("POST", "transactions", 201, {
            "description": "Privacy expense", "accountId": self.origin, "categoryId": rule["categoryId"],
            "date": f"{today}T00:00:00Z", "type": 2, "amount": 5,
        }, self.token)
        self.call("POST", "transfers", 201, self.transfer, self.token)
        self.call("PUT", "insights/limits", 204,
                  {"overallLimit": 55, "categories": []}, self.token)
        export = self.call("POST", "auth/privacy/export", 200, privacy, self.token)
        self.assertEqual(export["schemaVersion"], 1)
        self.assertEqual(export["profile"]["email"], credentials["email"])
        self.assertNotIn("passwordHash", export["profile"])
        self.assertCountEqual([item["id"] for item in export["accounts"]], [self.origin, self.destination])
        for collection in ("transactions", "transfers", "recurrences"):
            self.assertEqual(len(export[collection]), 1)
        self.assertEqual(export["limits"]["overallLimit"], 55)

        login = Request(f"{BASE_URL}/api/auth/login", json.dumps(credentials).encode(),
                        {"Content-Type": "application/json"}, method="POST")
        with urlopen(login, timeout=10) as response:
            cookie = response.headers["Set-Cookie"].split(";", 1)[0]
            second_token = json.loads(response.read())["token"]
        self.call("POST", "auth/privacy/delete", 204, privacy, self.token)
        self.call("GET", "accounts", 401, token=self.token)
        self.call("GET", "accounts", 401, token=second_token)
        refresh = Request(f"{BASE_URL}/api/auth/refresh", headers={
            "Cookie": cookie, "X-FinTrack-Session": "1",
        }, method="POST")
        with self.assertRaises(HTTPError) as failure:
            urlopen(refresh, timeout=10)
        with failure.exception as response:
            self.assertEqual(response.code, 401)
        self.call("POST", "auth/login", 400, credentials)
        self.assertEqual(self.call("GET", "accounts", 200, token=other)[0]["id"], other_id)

    def test_monthly_category_report_and_csv_match_the_selected_user_and_period(self):
        category = self.call("POST", "categories", 201, {"name": 'Bills, "fixed"', "type": 2}, self.token)
        now = datetime.now(timezone.utc)
        period = f"month={now.month}&year={now.year}"
        description = '=1+1, "utility"'
        self.call("POST", "transactions", 201, {
            "description": description, "amount": 80.25,
            "date": now.isoformat().replace("+00:00", "Z"), "type": 2,
            "accountId": self.origin, "categoryId": category["id"],
        }, self.token)
        path = f"reports/categories?{period}&type=2"
        totals = self.call("GET", path, 200, token=self.token)
        self.assertEqual(totals, [{
            "categoryId": category["id"], "categoryName": category["name"], "type": 2,
            "total": Decimal("80.25"), "count": 1,
        }])
        self.assertEqual(self.call("GET", path, 200, token=self.register()), [])
        self.call("GET", f"reports/categories?{period}&type=1", 200, token=self.token)
        export = Request(f"{BASE_URL}/api/reports/transactions.csv?{period}",
                         headers={"Authorization": f"Bearer {self.token}"})
        with urlopen(export, timeout=10) as response:
            self.assertEqual(response.headers.get_content_type(), "text/csv")
            records = list(csv.DictReader(io.StringIO(response.read().decode("utf-8-sig"))))
        self.assertEqual(len(records), 1)
        self.assertEqual(records[0]["category"], category["name"])
        self.assertEqual(records[0]["description"], "'" + description)
        self.assertEqual(records[0]["amount"], "80.25")
        self.call("GET", "reports/transactions.csv?month=13&year=2026", 400, token=self.token)

    def test_weekly_insights_and_monthly_limits_are_scoped_to_the_user(self):
        self.call("GET", "insights", 401)
        token = self.token
        other = self.register()
        category = self.call("POST", "categories", 201, {"name": "Meals", "type": 2}, token)
        limits = {"overallLimit": 100, "categories": [{"categoryId": category["id"], "limit": 80}]}
        self.call("PUT", "insights/limits", 400, limits, other)
        self.call("PUT", "insights/limits", 400, {**limits, "overallLimit": 0}, token)
        self.call("PUT", "insights/limits", 400,
                  {**limits, "categories": limits["categories"] * 2}, token)
        self.call("PUT", "insights/limits", 204, limits, token)

        today = datetime.now(ZoneInfo("America/Sao_Paulo")).date()
        monday = today - timedelta(days=today.weekday())

        def post(day, amount):
            self.call("POST", "transactions", 201, {
                "description": "Weekly spending test", "amount": amount, "date": f"{day}T00:00:00Z",
                "type": 2, "accountId": self.origin, "categoryId": category["id"],
            }, token)

        post(monday - timedelta(days=14), 11)
        post(monday - timedelta(days=7), 20)
        post(monday - timedelta(days=1), 41)
        post(today + timedelta(days=1), 500)
        historical_in_month = sum(
            amount for day, amount in [(monday - timedelta(days=14), 11),
                                       (monday - timedelta(days=7), 20), (monday - timedelta(days=1), 41)]
            if day.month == today.month
        )
        post(today, 80 - historical_in_month)
        result = self.call("GET", "insights", 200, token=token)
        self.assertEqual(len(result["weeks"]), 8)
        latest = result["weeks"][0]
        self.assertEqual(latest["start"], str(monday - timedelta(days=7)))
        self.assertEqual(latest["end"], str(monday - timedelta(days=1)))
        self.assertEqual((latest["total"], latest["previousTotal"], latest["count"]), (61, 11, 2))
        self.assertEqual((latest["smallCount"], latest["smallTotal"]), (1, 20))
        self.assertEqual([row["status"] for row in result["budgets"]], ["near", "reached"])
        self.assertEqual([row["amount"] for row in result["budgets"]], [80, 80])

        isolated = self.call("GET", "insights", 200, token=other)
        self.assertEqual(isolated["budgets"], [])
        self.assertEqual(isolated["weeks"][0]["total"], 0)
        self.call("PUT", "insights/limits", 204, {"overallLimit": None, "categories": []}, token)
        self.assertEqual(self.call("GET", "insights", 200, token=token)["budgets"], [])

    def test_transfer_history_paginates_with_stable_order_and_user_scope(self):
        created_ids = {
            self.call("POST", "transfers", 201, self.transfer, self.token)["id"]
            for _ in range(3)
        }
        pages = [
            self.call("GET", f"transfers/page?page={page}&pageSize=2", 200, token=self.token)
            for page in range(1, 4)
        ]
        self.assertEqual([len(result["items"]) for result in pages], [2, 1, 0])
        self.assertEqual([result["page"] for result in pages], [1, 2, 3])
        self.assertTrue(all(result["totalCount"] == 3 for result in pages))
        self.assertCountEqual(
            [item["id"] for result in pages for item in result["items"]],
            created_ids,
        )
        repeated = self.call("GET", "transfers/page?pageSize=2", 200, token=self.token)
        self.assertEqual(repeated, pages[0])
        other = self.call("GET", "transfers/page", 200, token=self.register())
        self.assertEqual(other, {"items": [], "page": 1, "pageSize": 20, "totalCount": 0})
        self.call("GET", "transfers/page?pageSize=101", 400, token=self.token)
        self.call("GET", "transfers/page?page=0", 400, token=self.token)
        self.call("GET", "transfers/page", 401)

    def test_transaction_page_filters_and_monthly_totals_cover_all_matching_rows(self):
        income_category = self.call(
            "POST", "categories", 201, {"name": "Income", "type": 1}, self.token
        )["id"]
        expense_category = self.call(
            "POST", "categories", 201, {"name": "Expense", "type": 2}, self.token
        )["id"]
        body = {
            "description": "Transaction integration test",
            "date": self.transfer["date"],
            "accountId": self.origin,
        }
        income_ids = [
            self.call(
                "POST", "transactions", 201,
                {**body, "amount": value, "type": 1, "categoryId": income_category},
                self.token,
            )["id"]
            for value in (15, 30)
        ]
        self.call(
            "POST", "transactions", 201,
            {**body, "amount": 5, "type": 2, "categoryId": expense_category},
            self.token,
        )
        self.call(
            "POST", "transactions", 201,
            {**body, "date": "2000-01-01T00:00:00Z", "amount": 100, "type": 1,
             "categoryId": income_category},
            self.token,
        )
        filters = self.monthly_path.split("?", 1)[1]
        url = f"transactions/page?{filters}&type=1&categoryId={income_category}&pageSize=1"
        pages = [self.call("GET", f"{url}&page={page}", 200, token=self.token) for page in (1, 2)]
        self.assertTrue(all(page["totalCount"] == 2 for page in pages))
        self.assertCountEqual([page["items"][0]["id"] for page in pages], income_ids)
        self.call("GET", "transactions/page?month=13", 400, token=self.token)

        summary = self.call("GET", self.monthly_path, 200, token=self.token)
        self.assertEqual((summary["totalIncome"], summary["totalExpense"], summary["finalBalance"]),
                         (45, 5, 40))
        overall = self.call("GET", "summaries/overall-balance", 200, token=self.token)
        self.assertEqual(overall, {"totalBalance": 1340, "accountsCount": 2})
        other_token = self.register()
        other = self.call("GET", "transactions/page", 200, token=other_token)
        self.assertEqual(other["totalCount"], 0)
        other_summary = self.call("GET", self.monthly_path, 200, token=other_token)
        self.assertEqual(other_summary["finalBalance"], 0)

    def test_transfer_and_reversal_preserve_total_balance_and_monthly_summary(self):
        monthly = self.call("GET", self.monthly_path, 200, token=self.token)
        created = self.call("POST", "transfers", 201, self.transfer, self.token)
        path = f"transfers/{created['id']}"
        self.assertEqual(self.call("GET", path, 200, token=self.token)["amount"], Decimal("125.50"))
        self.assert_balances(Decimal("874.50"), Decimal("325.50"))
        self.assertEqual(self.call("GET", self.monthly_path, 200, token=self.token), monthly)

        self.call("DELETE", path, 204, token=self.token)
        self.call("GET", path, 404, token=self.token)
        self.assert_balances(1000, 200)
        self.assertEqual(self.call("GET", self.monthly_path, 200, token=self.token), monthly)

    def test_concurrent_transfers_match_history_and_balances(self):
        request_count = 12
        start = Barrier(request_count, timeout=10)
        body = {**self.transfer, "amount": 1}

        def send_transfer(_):
            start.wait()
            return request("POST", "/api/transfers", body, self.token)

        with ThreadPoolExecutor(max_workers=request_count) as pool:
            responses = list(pool.map(send_transfer, range(request_count)))

        statuses = [status for status, _ in responses]
        self.assertTrue(all(status in (201, 409) for status in statuses), statuses)
        accepted = [transfer["id"] for status, transfer in responses if status == 201]
        self.assertTrue(accepted)
        history = self.call("GET", "transfers", 200, token=self.token)
        self.assertCountEqual([transfer["id"] for transfer in history], accepted)
        self.assert_balances(1000 - len(accepted), 200 + len(accepted))

        for _ in range(statuses.count(409)):
            self.call("POST", "transfers", 201, body, self.token)
        self.assert_balances(1000 - request_count, 200 + request_count)

    def test_invalid_transfers_and_cross_user_operations_leave_balances_unchanged(self):
        other_token = self.register()
        other_account = self.create_account(other_token, "Other user", 0)
        self.call("GET", "transfers", 401)

        for changes in (
            {"toAccountId": self.origin},
            {"toAccountId": other_account},
            {"amount": 0},
            {"amount": 0.001},
        ):
            with self.subTest(changes=changes):
                self.call("POST", "transfers", 400, {**self.transfer, **changes}, self.token)
        self.assertEqual(self.call("GET", "transfers", 200, token=self.token), [])
        self.assert_balances(1000, 200)

        transfer = self.call("POST", "transfers", 201, self.transfer, self.token)
        path = f"transfers/{transfer['id']}"
        self.assertEqual(self.call("GET", "transfers", 200, token=other_token), [])
        self.call("GET", path, 404, token=other_token)
        self.call("DELETE", path, 404, token=other_token)
        self.call("DELETE", f"accounts/{self.origin}", 409, token=self.token)
        self.assert_balances(Decimal("874.50"), Decimal("325.50"))


if __name__ == "__main__":
    unittest.main(verbosity=2)
