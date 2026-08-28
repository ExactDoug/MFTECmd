#!/usr/bin/env bash
# Invariant checker for the 2026-08-28 follow-up items.
# Run BEFORE starting work and BEFORE claiming an item is done.
# Any FAIL means stop and re-read; do not work around it.
# Usage:  ./verify.sh [env|repo|arrow|all]
set -uo pipefail
MFTECMD="$HOME/dev/projects/github/mftecmd"
NTFSIGHT="$HOME/dev/projects/github/ntfsight"
MFT="$HOME/dev/projects/github/mft"
pass=0; fail=0; warn=0
ok(){ printf "  \033[32mPASS\033[0m %s\n" "$1"; pass=$((pass+1)); }
no(){ printf "  \033[31mFAIL\033[0m %s\n" "$1"; fail=$((fail+1)); }
wa(){ printf "  \033[33mWARN\033[0m %s\n" "$1"; warn=$((warn+1)); }

check_env(){
  echo "== environment =="
  [ -x "$HOME/bin/winpath" ] && ok "winpath present (never hand bare wslpath -w to a Windows exe)" \
    || no "winpath MISSING - ext4 paths given to Windows tools will hit zone errors"
  [ -f "/mnt/c/Program Files/dotnet/dotnet.exe" ] && ok "dotnet.exe present (Windows-side only; there is no dotnet in WSL)" \
    || no "dotnet.exe not found - you cannot build"
  command -v dotnet >/dev/null && wa "a WSL 'dotnet' exists - do NOT use it; this project builds Windows-side" \
    || ok "no WSL dotnet (expected)"
  python3 -c "import pyarrow,duckdb" 2>/dev/null && ok "pyarrow + duckdb importable (needed to validate Arrow output)" \
    || no "pyarrow/duckdb missing - you cannot validate Arrow output"
  [ -r "/mnt/c/\$MFT" ] && wa "C:\\\$MFT readable - unexpected; are you elevated?" \
    || ok "C:\\\$MFT not readable (expected: no admin - use the git-tracked fixtures)"
}

check_repo(){
  echo "== repo state =="
  for d in "$MFTECMD" "$NTFSIGHT" "$MFT"; do
    n=$(basename "$d")
    [ -d "$d/.git" ] && ok "$n present on branch $(git -C "$d" branch --show-current)" || { no "$n missing"; continue; }
    if [ -n "$(git -C "$d" status --porcelain 2>/dev/null | grep -v '^??')" ]; then
      wa "$n has uncommitted tracked changes - know why before editing"
    fi
  done
  for f in tdungan xw NIST/DFR-16; do
    p="$MFTECMD/mft/MFT.Test/TestFiles/$f/\$MFT"
    if [ -f "$p" ]; then
      m=$(head -c4 "$p" | tr -d '\0')
      [ "$m" = "FILE" ] && ok "fixture $f present, FILE magic, $(stat -c%s "$p") bytes" || no "fixture $f bad magic: $m"
    else no "fixture $f MISSING"; fi
  done
  sub=$(git -C "$MFTECMD" ls-tree HEAD mft 2>/dev/null | awk '{print $3}')
  mm=$(git -C "$MFT" rev-parse origin/master 2>/dev/null)
  [ "$sub" = "$mm" ] && ok "submodule pinned at MFT master" || wa "submodule pins ${sub:0:7}, MFT master is ${mm:0:7} (item: bump pending)"
}

check_arrow(){
  echo "== arrow output validation =="
  exe="$MFTECMD/MFTECmd/bin/Debug/net9.0/MFTECmd.exe"
  if [ ! -f "$exe" ]; then wa "no Debug build yet - build before validating"; return; fi
  [ -x "$exe" ] && ok "MFTECmd.exe has exec bit" \
    || no "MFTECmd.exe NOT executable - run: chmod +x '$exe'  (required after EVERY build)"
}

case "${1:-all}" in
  env) check_env ;; repo) check_repo ;; arrow) check_arrow ;;
  *) check_env; check_repo; check_arrow ;;
esac
echo
printf "  %d pass, %d warn, %d fail\n" "$pass" "$warn" "$fail"
[ "$fail" -eq 0 ] || { echo "  STOP: resolve failures before proceeding."; exit 1; }
