#!/bin/bash
# Deploy des Kosmetik-Ladens. Vorher sichern.
set -eu
cd /opt/rouge-tcg

echo "=== Sicherung ==="
cp server.js server.js.bak-cosm
cp db.js db.js.bak-cosm
cp data/accounts.db data/accounts.db.bak-cosm
ls -la server.js.bak-cosm db.js.bak-cosm data/accounts.db.bak-cosm

echo ""
echo "=== Einspielen ==="
install -o rouge -g rouge -m 644 /tmp/stage-server.js    /opt/rouge-tcg/server.js
install -o rouge -g rouge -m 644 /tmp/stage-db.js        /opt/rouge-tcg/db.js
install -o rouge -g rouge -m 644 /tmp/stage-cosmetics.js /opt/rouge-tcg/cosmetics.js
ls -la server.js db.js ranks.js finishes.js cosmetics.js

echo ""
echo "=== Neustart ==="
systemctl restart rouge-tcg
sleep 3
systemctl is-active rouge-tcg

echo ""
echo "=== Log ==="
journalctl -u rouge-tcg -n 10 --no-pager | grep -v ExperimentalWarning | grep -v trace-warnings
