#!/bin/bash
# In-game acceptance test (docs/TESTS.md). Restores MelonPreferences.cfg afterwards.
# Phase 1 (FRESH=1 copies WORLD into the SI_DevTest save): PHASE=1 FRESH=1 WORLD=test4 tools/devrun.sh 75
# Phase 2 (reloads that save):                            PHASE=2 tools/devrun.sh 50
# Usage: PHASE=1|2 [WORLD=test4] [FRESH=1] devrun.sh QUIT_SECONDS ; prints "dev:" and error lines of the mod
QUIT=$1
S="${TMPDIR:-/tmp}"
GAME="${GAME:-/g/SteamLibrary/steamapps/common/StellarDrive Demo}"
C="$GAME/UserData/MelonPreferences.cfg"
W="${SAVES:-$USERPROFILE/AppData/LocalLow/CuriousOwlGames/StellarDrive/WorldSaves}"
L="$GAME/MelonLoader/Latest.log"
LOCK="$GAME/UserData/harness.lock"
# Other mods' test harnesses share the game: wait for theirs, hold the lock during ours
while [ -f "$LOCK" ] || tasklist //FI "IMAGENAME eq StellarDrive.exe" | grep -q StellarDrive; do sleep 2; done
echo "stellar-industries devrun" > "$LOCK"
trap 'rm -f "$LOCK"' EXIT
cp "$C" "$S/prefs.user.cfg"
if [ "${FRESH:-0}" = 1 ]; then rm -rf "$W/SI_DevTest"; mkdir -p "$W/SI_DevTest" && cp "$W/${WORLD:-test factory}/world.json" "$W/SI_DevTest/world.json"; fi
EXTRA="DevWorld = \"$(cygpath -m "$W")/SI_DevTest/world.json\"\nDevPhase = ${PHASE:-1}\nDevQuitSeconds = $QUIT.0\nLanguage = \"${LANGUAGE:-auto}\""
awk -v extra="$EXTRA" '
  /^\[/ { if (insec && !done) { n = split(extra, a, "\n"); for (i = 1; i <= n; i++) print a[i]; done = 1 } insec = ($0 == "[StellarIndustries]") }
  { if (insec && $0 ~ /^(Dev[A-Za-z]*|Language) =/) next; print }
  END { if (!done) { if (!insec) print "\n[StellarIndustries]"; n = split(extra, a, "\n"); for (i = 1; i <= n; i++) print a[i] } }' "$S/prefs.user.cfg" > "$C"
cmd //c start "" "steam://rungameid/4689730"
sleep 25
for i in $(seq 1 300); do
  if grep -q "dev: quitting" "$L" 2>/dev/null; then sleep 4; break; fi
  if ! tasklist //FI "IMAGENAME eq StellarDrive.exe" | grep -q StellarDrive; then echo "GAME EXITED"; break; fi
  sleep 2
done
while tasklist //FI "IMAGENAME eq StellarDrive.exe" | grep -q StellarDrive; do sleep 2; done
cp "$S/prefs.user.cfg" "$C"
grep -a -E "Stellar_Industries|Stellar Industries|dev:|ERROR|Error" "$L" | grep -a -v "TFAdvanced\]\|\[StellarDrive_Demo_TF" | cut -c1-400 | head -400
