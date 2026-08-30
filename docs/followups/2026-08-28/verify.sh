#!/usr/bin/env bash
# Invariant checker for the 2026-08-28 follow-up items.
# Run BEFORE starting work and BEFORE claiming an item is done, from the repo root.
# Any FAIL means stop and re-read; do not work around it.
#
# Revised 2026-08-30: the first version had ten ways to pass while broken. The worst let a
# STALE BINARY through a section titled "arrow output validation", so an agent could post
# evidence for code that was never compiled. Each fix below is marked [audit].
#
# Usage:  docs/followups/2026-08-28/verify.sh [env|repo|build|all]
set -uo pipefail
MFTECMD="$HOME/dev/projects/github/mftecmd"
NTFSIGHT="$HOME/dev/projects/github/ntfsight"
MFT_STANDALONE="$HOME/dev/projects/github/mft"
SUBMODULE="$MFTECMD/mft"                       # [audit d] the actual build input
EXPECTED_BRANCHES="master|docs/followups-2026-08-28|feat/|fix/|chore/"
pass=0; fail=0; warn=0
ok(){ printf "  \033[32mPASS\033[0m %s\n" "$1"; pass=$((pass+1)); }
no(){ printf "  \033[31mFAIL\033[0m %s\n" "$1"; fail=$((fail+1)); }
wa(){ printf "  \033[33mWARN\033[0m %s\n" "$1"; warn=$((warn+1)); }

check_env(){
  echo "== environment =="
  [ -x "$HOME/bin/winpath" ] && ok "winpath present (never hand bare wslpath -w to a Windows exe)" \
    || no "winpath MISSING - ext4 paths given to Windows tools will hit zone errors"
  [ -f "/mnt/c/Program Files/dotnet/dotnet.exe" ] && ok "dotnet.exe present (Windows-side only)" \
    || no "dotnet.exe not found - you cannot build"
  command -v dotnet >/dev/null && wa "a WSL 'dotnet' exists - do NOT use it" || ok "no WSL dotnet (expected)"
  # [audit g] VERIFY.md section 3 needs pandas; checking only pyarrow+duckdb gave a green light
  # followed by ImportError.
  python3 - <<'PY' 2>/dev/null && ok "pyarrow + duckdb + pandas importable" \
    || no "missing python deps - VERIFY.md section 3 needs pyarrow, duckdb AND pandas"
import pyarrow, duckdb, pandas
PY
  # [audit e] the old test passed when the path did not exist at all, so an unmounted
  # /mnt/c read as a healthy environment.
  if [ ! -e "/mnt/c/\$MFT" ]; then
    no "/mnt/c/\$MFT does not exist - is /mnt/c mounted? (an absent path is NOT 'protected')"
  elif [ -r "/mnt/c/\$MFT" ]; then
    wa "C:\\\$MFT readable - unexpected; are you elevated?"
  else
    ok "C:\\\$MFT exists and content is not readable (expected: no admin; use the fixtures)"
  fi
}

check_repo(){
  echo "== repo state =="
  for d in "$MFTECMD" "$NTFSIGHT" "$MFT_STANDALONE"; do
    n=$(basename "$d")
    [ -d "$d/.git" ] || { no "$n missing"; continue; }
    b=$(git -C "$d" branch --show-current)
    # [audit f] G0 claimed to catch a wrong branch; the old script only printed it.
    if [ -z "$b" ]; then
      no "$n is in DETACHED HEAD - commits here can be lost"
    elif printf '%s' "$b" | grep -qE "^($EXPECTED_BRANCHES)"; then
      ok "$n on branch $b"
    else
      no "$n on UNEXPECTED branch '$b' - confirm this is intended before editing"
    fi
    [ -n "$(git -C "$d" status --porcelain 2>/dev/null | grep -v '^??')" ] \
      && wa "$n has uncommitted tracked changes - know why before editing"
  done

  for f in tdungan xw NIST/DFR-16; do
    p="$SUBMODULE/MFT.Test/TestFiles/$f/\$MFT"
    if [ -f "$p" ]; then
      sz=$(stat -c%s "$p")
      if [ "$((sz % 1024))" -ne 0 ]; then
        no "fixture $f size $sz is not a multiple of 1024 - truncated?"
      elif [ "$(head -c4 "$p" | tr -d '\0')" = "FILE" ]; then
        ok "fixture $f present, FILE magic, $sz bytes ($((sz/1024)) slots)"
      else no "fixture $f bad magic"; fi
    else no "fixture $f MISSING (looked in the SUBMODULE, not the standalone clone)"; fi
  done
  wa "a duplicate fixture set exists in $MFT_STANDALONE - confirm which repo you are reading"

  # [audit c,d] the old check compared the pin against the STANDALONE clone's origin/master,
  # and false-passed when both sides resolved empty.
  sub_pin=$(git -C "$MFTECMD" ls-tree HEAD mft 2>/dev/null | awk '{print $3}')
  sub_head=$(git -C "$SUBMODULE" rev-parse HEAD 2>/dev/null)
  if [ -z "$sub_pin" ] || [ -z "$sub_head" ]; then
    no "submodule state unresolvable (pin='$sub_pin' head='$sub_head') - not a pass"
  elif [ "$sub_pin" = "$sub_head" ]; then
    ok "submodule checkout matches the pin (${sub_pin:0:7})"
  else
    no "submodule checkout ${sub_head:0:7} != pinned ${sub_pin:0:7} - line numbers in mft/ may not match what builds"
  fi
}

check_build(){
  # [audit a] renamed: this section validates the BUILD ARTIFACT. It does not validate
  # Arrow output - only VERIFY.md section 3 does that, against a baseline.
  echo "== build artifact =="
  exe="$MFTECMD/MFTECmd/bin/Debug/net9.0/MFTECmd.exe"
  if [ ! -f "$exe" ]; then
    # [audit h] was WARN + exit 0, letting an agent proceed with nothing built.
    no "no Debug build at $exe - build before validating anything"
    return
  fi
  [ -x "$exe" ] && ok "MFTECmd.exe has exec bit" \
    || no "MFTECmd.exe NOT executable - chmod +x (required after EVERY build)"

  # [audit b] THE BIG ONE. There was no freshness check at all, so a stale binary passed
  # every gate and produced 'evidence' for code that was never compiled.
  newest=$(find "$MFTECMD/MFTECmd" "$SUBMODULE/MFT" -name '*.cs' -newer "$exe" -print -quit 2>/dev/null)
  if [ -n "$newest" ]; then
    no "STALE BINARY: $(basename "$newest") is newer than MFTECmd.exe - REBUILD before collecting evidence"
  else
    ok "MFTECmd.exe is newer than every .cs in MFTECmd/ and mft/MFT/"
  fi
}

case "${1:-all}" in
  env) check_env ;; repo) check_repo ;; build) check_build ;;
  *) check_env; check_repo; check_build ;;
esac
echo
printf "  %d pass, %d warn, %d fail\n" "$pass" "$warn" "$fail"
[ "$fail" -eq 0 ] || { echo "  STOP: resolve failures before proceeding."; exit 1; }
