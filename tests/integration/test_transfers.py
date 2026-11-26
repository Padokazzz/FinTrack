import json
import os
import secrets
import unittest
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timezone
from decimal import Decimal
from threading import Barrier
from time import monotonic, sleep
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

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


class TransferIntegrationTests(unittest.TestCase):
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
        self.assertEqual(status, expected, f"{method} /api/{path}")
        return result

    def register(self):
        credentials = {
            "email": f"fintrack-ci-{secrets.token_hex(8)}@example.com",
            "password": secrets.token_urlsafe(24),
        }
        self.call("POST", "auth/register", 200, {"name": "Integration Test", **credentials})
        return self.call("POST", "auth/login", 200, credentials)["token"]

    def create_account(self, token, name, balance):
        body = {"name": name, "type": 1, "initialBalance": balance}
        return self.call("POST", "accounts", 201, body, token)["id"]

    def setUp(self):
        self.token = self.register()
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
