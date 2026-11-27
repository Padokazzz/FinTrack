import os
import re
import secrets
import subprocess
import unittest
from email import policy
from email.parser import BytesParser
from pathlib import Path

from test_transfers import request


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

        signup = post("register", {"name": "Email test", **credentials}, 202)
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
