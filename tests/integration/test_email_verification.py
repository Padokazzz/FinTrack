import json
import os
import re
import secrets
import subprocess
import unittest
from email import policy
from email.parser import BytesParser
from pathlib import Path

from urllib.error import HTTPError
from urllib.request import Request, urlopen

from test_transfers import BASE_URL, request


@unittest.skipUnless(os.getenv("FINTRACK_EMAIL_TEST_PICKUP"), "Requires a local test mail pickup directory")
class EmailVerificationTests(unittest.TestCase):
    def test_signup_reverification_and_revocation(self):
        pickup = Path(os.environ["FINTRACK_EMAIL_TEST_PICKUP"])
        credentials = {"email": f"{secrets.token_hex(12)}@example.com", "password": secrets.token_urlsafe(24)}

        def post(route, data, expected):
            status, body = request("POST", f"/api/auth/{route}", data)
            self.assertEqual(status, expected)
            return body

        def code_from_mail():
            for path in sorted(pickup.glob("*.eml"), key=lambda item: item.stat().st_mtime_ns, reverse=True):
                msg = BytesParser(policy=policy.default).parsebytes(path.read_bytes())
                if str(msg["To"]) == credentials["email"]:
                    return re.search(r"\b([0-9]{6})\b", msg.get_body().get_content())[1]
            self.fail("Security email not delivered to pickup directory")

        signup = post("register", {"name": "Email test", "confirmPassword": credentials["password"], **credentials}, 202)
        self.assertNotIn("token", signup)
        post("login", credentials, 429)
        code = code_from_mail()
        attempt = {"challengeId": signup["challengeId"], "code": code}
        post("verify-email", {**attempt, "code": "000000" if code != "000000" else "000001"}, 400)
        confirmed = post("verify-email", attempt, 200)
        post("verify-email", attempt, 400)
        self.assertEqual(request("GET", "/api/accounts", token=confirmed["token"])[0], 200)
        post("login", credentials, 200)

        user_id = confirmed["userId"]

        def age_state(field, days):
            assignment = f"""UPDATE "EmailVerifications" SET "{field}" = now() - interval '{days} days'"""
            if field != "SentAt":
                assignment += """, "SentAt" = now() - interval '2 minutes'"""
            subprocess.run([os.environ["FINTRACK_EMAIL_TEST_PSQL"], "-v", "ON_ERROR_STOP=1",
                            "-h", os.environ["FINTRACK_EMAIL_TEST_PG_SOCKET"], "-U", "fintrack", "-d", "postgres",
                            "-c", assignment + f""" WHERE "UserId" = '{user_id}'"""],
                           check=True, capture_output=True)

        age_state("LastActivityAt", 15)
        self.assertEqual(request("GET", "/api/accounts", token=confirmed["token"])[0], 401)
        login = post("login", credentials, 202)
        renewed = post("verify-email", {**login, "code": code_from_mail()}, 200)
        age_state("VerifiedAt", 31)
        self.assertEqual(request("GET", "/api/accounts", token=renewed["token"])[0], 401)
        login = post("login", credentials, 202)
        code = code_from_mail()
        attempt = {**login, "code": code}
        wrong = "000000" if code != "000000" else "000001"
        for _ in range(5):
            post("verify-email", {**attempt, "code": wrong}, 400)
        post("verify-email", attempt, 400)
        age_state("SentAt", 1)
        login = post("login", credentials, 202)
        code = code_from_mail()
        age_state("SentAt", 1)
        post("verify-email", {**login, "code": code}, 400)

    def test_password_recovery_requires_email_code_and_revokes_sessions(self):
        pickup = Path(os.environ["FINTRACK_EMAIL_TEST_PICKUP"])
        email = f"{secrets.token_hex(12)}@example.com"
        old = secrets.token_urlsafe(24)
        new = secrets.token_urlsafe(24)

        def post(route, body, expected):
            status, result = request("POST", f"/api/auth/{route}", body)
            self.assertEqual(status, expected, (route, result))
            return result

        def latest_code():
            for path in sorted(pickup.glob("*.eml"), key=lambda item: item.stat().st_mtime_ns, reverse=True):
                msg = BytesParser(policy=policy.default).parsebytes(path.read_bytes())
                if str(msg["To"]) == email:
                    return re.search(r"\b([0-9]{6})\b", msg.get_body().get_content())[1]
            self.fail("Verification message missing")

        registration = {"name": "Recovery test", "email": email, "password": old}
        post("register", {**registration, "confirmPassword": "mismatch"}, 400)
        challenge = post("register", {**registration, "confirmPassword": old}, 202)
        signed_in = post("verify-email", {**challenge, "code": latest_code()}, 200)
        login_request = Request(f"{BASE_URL}/api/auth/login", json.dumps({"email": email, "password": old}).encode(),
                                {"Content-Type": "application/json"}, method="POST")
        with urlopen(login_request, timeout=10) as response:
            login_cookie = response.headers["Set-Cookie"].split(";", 1)[0]

        unknown = post("forgot-password", {"email": "unknown@example.com"}, 202)
        recovery = post("forgot-password", {"email": email}, 202)
        self.assertEqual(set(unknown), set(recovery))
        code = latest_code()
        reset = {**recovery, "code": code, "password": new, "confirmPassword": new}
        post("reset-password", {**reset, "confirmPassword": old}, 400)
        post("reset-password", {**reset, "code": "000000" if code != "000000" else "000001"}, 400)
        post("reset-password", reset, 204)
        post("reset-password", reset, 400)
        self.assertEqual(request("GET", "/api/accounts", token=signed_in["token"])[0], 401)
        refresh = Request(f"{BASE_URL}/api/auth/refresh", headers={
            "Cookie": login_cookie, "X-FinTrack-Session": "1",
        }, method="POST")
        with self.assertRaises(HTTPError) as error:
            urlopen(refresh, timeout=10)
        with error.exception as response:
            self.assertEqual(response.code, 401)
        post("login", {"email": email, "password": old}, 400)
        auth = post("login", {"email": email, "password": new}, 200)
        self.assertEqual(request("GET", "/api/accounts", token=auth["token"])[0], 200)
