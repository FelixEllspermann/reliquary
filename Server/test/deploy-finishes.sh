#!/bin/bash
# Deploy der Karten-Finishes. Die Sammlungstabelle wird umgebaut — vorher sichern.
set -eu
cd /opt/rouge-tcg

echo "=== Sicherung ==="
cp server.js server.js.bak-finishes
cp db.js db.js.bak-finishes
cp data/accounts.db data/accounts.db.bak-finishes
cp data/accounts.db-wal data/accounts.db-wal.bak-finishes 2>/dev/null || true
ls -la server.js.bak-finishes db.js.bak-finishes data/accounts.db.bak-finishes

echo ""
echo "=== Einspielen ==="
install -o rouge -g rouge -m 644 /tmp/stage-server.js   /opt/rouge-tcg/server.js
install -o rouge -g rouge -m 644 /tmp/stage-db.js       /opt/rouge-tcg/db.js
install -o rouge -g rouge -m 644 /tmp/stage-finishes.js /opt/rouge-tcg/finishes.js
ls -la server.js db.js ranks.js finishes.js

echo ""
echo "=== Neustart ==="
systemctl restart rouge-tcg
sleep 3
systemctl is-active rouge-tcg

echo ""
echo "=== Log ==="
journalctl -u rouge-tcg -n 12 --no-pager | grep -v ExperimentalWarning | grep -v trace-warnings
