#!/bin/bash
# Gibt einen Staging-Port frei. Beendet ausdrücklich NUR Prozesse, die nicht
# aus /opt/rouge-tcg laufen — die Produktion bleibt unangetastet.
PORT="${1:-7779}"
FOUND=0
for pid in $(ls /proc | grep -E '^[0-9]+$'); do
  [ -r "/proc/$pid/cwd" ] || continue
  cwd=$(readlink /proc/$pid/cwd 2>/dev/null)
  case "$cwd" in
    /opt/rouge-tcg*) continue ;;                       # Produktion: Finger weg
    /tmp/rouge-*)    ;;                                # Staging
    *)               continue ;;
  esac
  if grep -qa "server.js" "/proc/$pid/cmdline" 2>/dev/null; then
    echo "beende Staging-Prozess $pid (cwd $cwd)"
    kill "$pid" 2>/dev/null
    FOUND=1
  fi
done
sleep 1
[ "$FOUND" = 0 ] && echo "kein Staging-Prozess gefunden"
echo "--- Produktion ---"
systemctl is-active rouge-tcg
