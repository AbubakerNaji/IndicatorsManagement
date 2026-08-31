# ADR-0005 — Back stateless JWTs with a server-side session table

**Status:** Accepted · **Date:** 2026-03-30 (recorded retrospectively 2026-08-22)

## Context

A JWT is self-contained: the server validates the signature and expiry without consulting
any store. That is its main advantage, and it produces two properties this system cannot
accept:

- **A token cannot be revoked.** Logout is a client-side gesture; a copied token keeps
  working until it expires. So does the token of a user who was just deactivated, or whose
  account was compromised.
- **There is no idle timeout.** A JWT expires at a fixed time regardless of whether the
  user has been active. An unattended session cannot be ended early.

The security requirements for this system specify a 30-minute inactivity timeout and the
ability to terminate sessions. Both are stated obligations, not preferences, for a
government system holding pre-publication economic data.

Shortening the token lifetime helps only marginally: a one-minute JWT would require
constant refreshing and still leaves a revocation window.

## Decision

Keep JWTs for authentication, and require every authenticated request to correspond to a
live row in `user_sessions`.

Login issues the token **and** inserts a row holding it, with `IpAddress`, `UserAgent`,
`LastActivity`, and `ExpiresAt`. `SessionValidationMiddleware` runs after
`UseAuthorization` and requires:

1. a `user_sessions` row whose `SessionToken` matches the presented token,
2. `ExpiresAt` in the future,
3. `LastActivity` within `SessionTimeout_Minutes` (default 30, from configuration),

then refreshes `LastActivity`. Any failure deletes the row and returns 401 with an Arabic
explanation. Logout deletes the row. `SessionCleanupJob` sweeps expired rows hourly.

JWT validation itself uses `ClockSkew = TimeSpan.Zero`, so expiry is exact rather than the
default five-minute grace.

## Consequences

**Good.** Logout is real — the token stops working immediately. A compromised session can
be terminated by deleting one row. Idle timeout is enforced server-side and is
configurable at runtime without redeploying. `IpAddress` and `UserAgent` on each session
give a basis for anomaly investigation.

**Bad.** The system is no longer stateless. Every authenticated request costs a database
read **and** a write. That is the price, and it is charged on every request rather than
only on sensitive ones (finding **P1**).

**Bad.** `SessionToken` stores the complete signed JWT in plaintext. Read access to that
table is equivalent to being able to impersonate every logged-in user (finding **S2**).
Storing a SHA-256 hash would preserve every property of this decision — lookup is an
equality comparison either way — and should be done.

**Bad.** The column is `nvarchar(2000)` and indexed, which exceeds SQL Server's 1700-byte
nonclustered index key limit, so the index cannot serve seeks on long tokens. Hashing
fixes this too: 64 characters, comfortably indexable.

**Neutral.** Horizontal scaling still works, since the session store is the shared
database — but it makes the database the bottleneck for every request rather than only
for data access.

## Alternatives considered

**Pure stateless JWTs with a short lifetime plus refresh tokens.** The conventional
answer. Rejected: it shrinks the revocation window without closing it, and it does not
provide an idle timeout at all — a refresh token can be presented by an idle client just
as easily as an active one.

**A denylist of revoked tokens.** Cheaper than a full session table and revocation-only.
Rejected: it gives no idle timeout, and the denylist must be retained for the full token
lifetime anyway, so the storage saving is smaller than it looks.

**Distributed cache (Redis) instead of a table.** Better performance and natural TTL
semantics for exactly this data. Rejected for now as an additional piece of
infrastructure to deploy and operate, for a system whose load does not yet require it.
This is the right destination once **P1** becomes a real problem.
