-- Provision once in a dedicated pilot database, after applying EF migrations.
-- Run as the migration administrator through psql with ON_ERROR_STOP enabled.
BEGIN;
CREATE ROLE fintrack_runtime LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO fintrack_runtime;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
    public."Users", public."Accounts", public."Categories", public."Transactions",
    public."Transfers", public."RefreshSessions", public."RecurringRules",
    public."RecurringOccurrences", public."SpendingBudgets", public."EmailVerifications"
TO fintrack_runtime;
COMMIT;

-- psql prompts for the password without placing it in the SQL source.
\password fintrack_runtime
