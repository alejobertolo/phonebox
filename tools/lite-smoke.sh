#!/usr/bin/env bash
# Minimal CI-only smoke test; never run on a personal machine or upstream installer.
set -euo pipefail
agent="${1:?pass built local agent path}"
workdir="$(mktemp -d)"
pid=""
cleanup() {
  if [[ -n "$pid" ]]; then kill "$pid" 2>/dev/null || true; wait "$pid" 2>/dev/null || true; fi
  rm -rf -- "$workdir"
}
trap cleanup EXIT
"$agent" --config "$workdir/config.json" > "$workdir/agent.log" 2>&1 &
pid="$!"
base="http://127.0.0.1:8765"
ready=0
for attempt in $(seq 1 25); do
  if curl -fsS --max-time 2 "$base/api/state" > "$workdir/state.json" 2>/dev/null; then
    ready=1
    break
  fi
  sleep 1
done
if [[ "$ready" -ne 1 ]]; then
  cat "$workdir/agent.log"
  echo "Agent never served local API" >&2
  exit 1
fi
sleep 2
curl -fsS "$base/api/state" > "$workdir/state.json"
curl -fsS "$base/api/catalog" > "$workdir/catalog.json"
python3 - "$workdir" <<'PY'
import json, pathlib, sys
folder=pathlib.Path(sys.argv[1])
state=json.loads((folder/"state.json").read_text())
pc=state.get("pc")
assert pc is not None, state
assert pc["ramTotalGb"] > 0, pc
assert "cpu" in pc and "netDownBps" in pc and "disks" in pc, pc
assert not any(k in state for k in ("sessions", "processes", "apps", "tokens")), state
catalog=json.loads((folder/"catalog.json").read_text())
assert [t["id"] for t in catalog["templates"]] == ["system"], catalog
assert set(w["type"] for w in catalog["widgets"]) <= {"chart", "meters", "stat", "gauge"}
print("PASS: CPU/RAM/network/disk only and safe widget catalog")
PY
for route in /api/update /api/stream /api/procs /api/run/test "/api/proxy?url=https://example.com"; do
  status="$(curl -sS -o /dev/null -w '%{http_code}' --max-time 4 "$base$route")"
  if [[ "$status" != 403 ]]; then echo "FAIL GET $route expected 403, got $status" >&2; exit 1; fi
done
for verb in POST PUT DELETE; do
  status="$(curl -sS -X "$verb" -o /dev/null -w '%{http_code}' --max-time 4 "$base/api/settings")"
  if [[ "$status" != 403 ]]; then echo "FAIL $verb /api/settings expected 403, got $status" >&2; exit 1; fi
done
echo "PASS: forbidden remote actions, proxy and write routes return HTTP 403"
