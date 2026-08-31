# Deploying to the internal server

The stack runs on **192.168.1.170** behind pfSense HAProxy. The server pulls
pre-built images from **GHCR**, so nothing is compiled on the box.

## One-time setup

```bash
# on the server (192.168.1.170)
sudo apt-get update && sudo apt-get install -y docker.io docker-compose-plugin git

# clone the repo
git clone https://github.com/AbubakerNaji/IndicatorsManagement.git
cd IndicatorsManagement

# create the environment file — NEVER commit this
cp .env.example .env
nano .env   # fill in DB_SA_PASSWORD, JWT_SECRET_KEY, ADMIN_PASSWORD, SMTP if used

# make sure ports 5000 (API) and 8080 (Web) are free
```

## Deploy / update

```bash
./deploy/deploy.sh              # pulls latest images and restarts the stack
./deploy/deploy.sh --logs        # same, then tails logs
./deploy/deploy.sh --reseed      # wipes ./data/db then restarts (destroys data)
```

## Data lives on disk

| Path                | What                                            |
|---------------------|-------------------------------------------------|
| `./data/db`         | SQL Server data files (bind-mounted to db)       |
| `./data/uploads`    | Attachment files (bind-mounted to api)           |
| `./data/logs`       | API logs (Serilog output)                        |

Back up by rsyncing `./data/`. No Docker volume archaeology needed.

## pfSense HAProxy pointers

The Docker stack exposes **plain HTTP**. TLS termination and hostname routing
are pfSense's job.

| Frontend/Backend | Server address  | Backend host   |
|------------------|-----------------|----------------|
| `indicators-web` | 192.168.1.170   | port **8080**  |
| `indicators-api` | 192.168.1.170   | port **5000**  |

Typical HAProxy layout:

- Public FQDN → HAProxy frontend on 443 (TLS cert managed by pfSense/ACME)
  - ACL `path_beg /api` → `indicators-api` backend
  - default → `indicators-web` backend

Or two separate FQDNs if preferred (e.g. `indicators.example.gov.ly` and
`api.indicators.example.gov.ly`).

## First login

- URL: whatever hostname pfSense fronts the stack with, or
  `http://192.168.1.170:8080` from inside the LAN.
- Username: `admin`
- Password: value of `ADMIN_PASSWORD` in `.env` (falls back to `Admin@123456`).

**Change it immediately after first login.**

## Verifying the audit hash chain (S10)

Any `Auditor` or `Super_Admin` can call:

```
GET /api/v1/audit-logs/verify-chain
```

Response payload:

```json
{
  "success": true,
  "data": {
    "totalRows": 1234,
    "isValid": true,
    "firstBrokenRowId": null,
    "breakReason": null,
    "checkedAt": "2026-…"
  }
}
```

If someone tampers with the `audit_log` table directly, `isValid` becomes
`false` and `firstBrokenRowId` points at the first suspicious row.

## Rolling back

```bash
docker compose -f docker-compose.prod.yml pull  # picks up newer :latest tags
# to pin a specific version:
docker pull ghcr.io/abubakernaji/indicatorsmanagement-api:<git-sha>
# then edit docker-compose.prod.yml to reference that tag and re-up.
```
