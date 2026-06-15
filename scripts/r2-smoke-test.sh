#!/usr/bin/env bash
# Live smoke test for the R2-backed asset upload endpoint.
#
# Run this on the API host (or any machine that can reach the API) AFTER the R2
# env vars are configured and the API is running with AssetStorage__Provider=R2.
#
# Usage:
#   BASE=https://api.hypegrid.co.za \
#   ADMIN_EMAIL=proficient@hypegrid.co.za ADMIN_PASSWORD='Profi384!' \
#   bash scripts/r2-smoke-test.sh
#
#   # or skip login by passing a token directly:
#   BASE=http://localhost:5247 TOKEN='eyJ...' bash scripts/r2-smoke-test.sh
#
# It uploads a real PNG for category=deal and category=hero-desktop, prints the
# returned public URLs, then GETs each URL to confirm it opens publicly (200).
set -euo pipefail

BASE="${BASE:-http://localhost:5247}"
ADMIN_EMAIL="${ADMIN_EMAIL:-admin@hypegrid.co.za}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-ChangeMe123!}"

say() { printf '\n\033[1;36m== %s ==\033[0m\n' "$1"; }

# A real 8x8 PNG (valid magic bytes + payload).
TMP="$(mktemp -d)"; trap 'rm -rf "$TMP"' EXIT
base64 -d > "$TMP/sample.png" <<'B64'
iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAYAAADED76LAAAAFklEQVR4nGNgYGD4z8DAwMgABXAGAAhEAQE5z3T7AAAAAElFTkSuQmCC
B64

if [ -z "${TOKEN:-}" ]; then
  say "Logging in as $ADMIN_EMAIL"
  TOKEN="$(curl -s -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' \
    -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}" \
    | python -c 'import sys,json;print(json.load(sys.stdin)["data"]["access_token"])')"
fi
[ -n "$TOKEN" ] || { echo "No token — login failed."; exit 1; }

upload() {
  local category="$1"
  say "Upload category=$category"
  local resp url
  resp="$(curl -s -X POST "$BASE/api/admin/assets/upload" \
    -H "Authorization: Bearer $TOKEN" \
    -F "file=@$TMP/sample.png;type=image/png" \
    -F "category=$category")"
  echo "$resp" | python -m json.tool || { echo "Non-JSON response:"; echo "$resp"; exit 1; }
  url="$(echo "$resp" | python -c 'import sys,json;print(json.load(sys.stdin).get("data",{}).get("url",""))')"
  if [ -n "$url" ]; then
    echo "Public GET → $(curl -s -o /dev/null -w 'HTTP %{http_code} type=%{content_type}' "$url")  $url"
  fi
}

upload deal
upload hero-desktop

say "Done. Verify the two objects now exist in the R2 bucket (hero/desktop/… and deals/…)."
