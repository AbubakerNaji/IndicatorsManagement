#!/usr/bin/env bash
# =============================================================================
# deploy.sh — pull the latest images from GHCR and (re)start the stack.
#
# Runs on the server (192.168.1.170). pfSense HAProxy fronts this stack, so we
# just expose plain HTTP on the ports docker-compose.prod.yml declares.
#
#   ./deploy/deploy.sh              # first-time or update
#   ./deploy/deploy.sh --logs        # follow logs after starting
#   ./deploy/deploy.sh --reseed      # DROP the DB volume before starting
# =============================================================================
set -euo pipefail

cd "$(dirname "$0")/.."

if [[ ! -f .env ]]; then
    echo "❌ .env missing. Copy .env.example → .env and fill it in first."
    exit 1
fi

mkdir -p data/db data/uploads data/logs

echo "▶ Pulling latest images…"
docker compose -f docker-compose.prod.yml pull

if [[ "${1:-}" == "--reseed" ]]; then
    echo "⚠  Reseeding — this will DELETE ./data/db (all indicator data)."
    read -rp "Type YES to confirm: " confirm
    [[ "$confirm" == "YES" ]] || { echo "Aborted."; exit 1; }
    docker compose -f docker-compose.prod.yml down
    sudo rm -rf ./data/db
    mkdir -p data/db
    shift
fi

echo "▶ Starting stack…"
docker compose -f docker-compose.prod.yml up -d

echo "▶ Waiting for API to become healthy…"
for i in {1..60}; do
    if curl -fsS http://localhost:5000/health >/dev/null 2>&1; then
        echo "✓ API is up on http://localhost:5000"
        break
    fi
    sleep 2
done

echo
echo "Stack is running:"
echo "  Frontend (HTTP): http://192.168.1.170:8080"
echo "  API (HTTP):      http://192.168.1.170:5000"
echo "  Health:          http://192.168.1.170:5000/health"
echo
echo "Point pfSense HAProxy at those ports."
echo "Default admin login: admin / Admin@123456  — CHANGE IT AFTER FIRST LOGIN."

if [[ "${1:-}" == "--logs" ]]; then
    docker compose -f docker-compose.prod.yml logs -f
fi
