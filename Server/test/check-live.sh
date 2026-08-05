#!/bin/bash
# Prüft, ob der Produktionsserver erreichbar ist und sauber antwortet.
echo "=== Dienst ==="
systemctl is-active rouge-tcg
ss -tlnp 2>/dev/null | grep 7777 || echo "  Port 7777 hört NICHT"

echo ""
echo "=== Handschlag über WebSocket ==="
cd /opt/rouge-tcg && node --input-type=module -e "
import { WebSocket } from 'ws';
const ws = new WebSocket('ws://127.0.0.1:7777');
const timer = setTimeout(() => { console.log('  FAIL keine Antwort'); process.exit(1); }, 8000);
ws.on('open', () => console.log('  ok   Verbindung steht'));
ws.on('error', e => { console.log('  FAIL ' + e.message); clearTimeout(timer); process.exit(1); });
ws.on('message', raw => {
  const m = JSON.parse(raw);
  if (m.t === 'welcome') { console.log('  ok   welcome empfangen'); return ws.send(JSON.stringify({ t: 'login', name: 'DiaPony', pass: '__falsch__' })); }
  if (m.t === 'error') { console.log('  ok   Server antwortet auf Login: \"' + m.msg + '\"'); clearTimeout(timer); process.exit(0); }
  if (m.t === 'auth_ok') { console.log('  ?    unerwartet eingeloggt'); clearTimeout(timer); process.exit(0); }
});
" 2>&1 | grep -v ExperimentalWarning | grep -v trace-warnings

echo ""
echo "=== Letzte Logzeilen ==="
journalctl -u rouge-tcg -n 12 --no-pager | grep -v ExperimentalWarning | grep -v trace-warnings
