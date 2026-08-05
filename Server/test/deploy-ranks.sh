#!/bin/bash
# Deploy der Rangleiter. Sichert vorher server.js, db.js und die Datenbank.
set -eu
cd /opt/rouge-tcg

echo "=== Sicherung ==="
cp server.js server.js.bak-ranks
cp db.js db.js.bak-ranks
sqlite3 data/accounts.db ".backup '/opt/rouge-tcg/data/accounts.db.bak-ranks'" 2>/dev/null \
  || cp data/accounts.db data/accounts.db.bak-ranks
ls -la server.js.bak-ranks db.js.bak-ranks data/accounts.db.bak-ranks

echo ""
echo "=== Einspielen ==="
install -o rouge -g rouge -m 644 /tmp/stage-server.js /opt/rouge-tcg/server.js
install -o rouge -g rouge -m 644 /tmp/stage-db.js     /opt/rouge-tcg/db.js
install -o rouge -g rouge -m 644 /tmp/stage-ranks.js  /opt/rouge-tcg/ranks.js
ls -la server.js db.js ranks.js

echo ""
echo "=== Neustart ==="
systemctl restart rouge-tcg
sleep 3
systemctl is-active rouge-tcg

echo ""
echo "=== Log ==="
journalctl -u rouge-tcg -n 15 --no-pager | grep -v ExperimentalWarning | grep -v trace-warnings
