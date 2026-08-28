# Monitoring protocol — catching a wrong turn early

These items are trap-laden enough that an agent can spend a long time going confidently in
the wrong direction. This file defines **where it must stop and report**, **what makes it
wrong**, and **how to pull it back**.

The goal is to bound wasted effort to one gate, not one session.

## Hard gates

An agent must post its output and **wait** at each gate. Cheap to check, expensive to skip.

| Gate | When | Must report | Catches |
|---|---|---|---|
| **G0 — Environment** | Before any work | `./verify.sh` output | Missing tooling, wrong branch, missing fixtures, exec-bit — before a single edit |
| **G1 — Restatement** | Before planning | Item number, repo, target branch, and the top 3 traps *in its own words* | Wrong item, wrong repo, skimmed the doc |
| **G2 — Plan** | Before editing | Files it intends to touch with line numbers, re-grepped now | Stale line numbers, scope creep, editing the wrong copy of a duplicated file |
| **G3 — Evidence** | Before claiming done | Actual command output proving the fix, not a description | "Should work", structure-only validation, unrun tests |
| **G4 — Diff** | Before commit | `git diff --stat` + confirmation nothing unrelated moved | Bundled changes, accidental reverts, formatting churn |

**G3 is the one that matters most.** The dictionary-encoding bug passed row count and file
size and was still corrupt. Structural validity is not correctness.

## Tripwires — stop immediately

If any of these becomes true, stop and report rather than working around it:

1. A verification step is about to be skipped because "it obviously works".
2. The fix requires editing a file the item does not name.
3. A number is about to be quoted that is not in `FACTS.md`.
4. A citation ID from one report is about to be resolved with the other report's bibliography.
5. A find-and-replace is about to run across `docs/` (see item 06 — one match is a false positive).
6. `git clean`, `git checkout --`, or a branch reset is about to run in ntfsight (item 09 —
   irreplaceable untracked artifacts live there).
7. A commit is about to land on `docs/consolidate-documentation` (retired) or directly on `master`.
8. `.docx` output is about to be treated as a citation source of truth.
9. A benchmark number is about to be reported from a Debug build.
10. A fixture-derived figure is about to be extrapolated to a real volume without saying so.

## Divergence signals — you are probably off track if…

- You are reading `Program.cs` end to end. Every item names its lines; re-grep, don't browse.
- You are re-deriving the citation mapping. It is solved and stored in the TSVs.
- You are installing packages. Everything needed is present; `verify.sh` proves it.
- You are trying to read a live `C:\$MFT`. There is no admin. Use the fixtures.
- You are editing generated markdown by hand instead of the TSV + `repair.py`.
- You have written more than ~50 lines for an item marked **S**.
- You are testing on `xw` or `NIST` for anything batch-related — both are single-batch and
  cannot reproduce multi-batch bugs.
- You are about to say "I couldn't reproduce it, so it's probably fine."

## Agent briefing — paste this when launching

```
Read, in order:
  docs/followups/2026-08-28/README.md
  docs/followups/2026-08-28/GLOSSARY.md      (terms that have caused real errors)
  docs/followups/2026-08-28/FACTS.md         (the only numbers you may quote)
  docs/followups/2026-08-28/TRAPS.md
  docs/followups/2026-08-28/<your item>.md   (especially its ranked mistakes)

Then run:  docs/followups/2026-08-28/./verify.sh
Post the output. Do not proceed on any FAIL.

Work ONE item. Stop and report at gates G1-G4 in CHECKPOINTS.md.
Do not quote a number that is not in FACTS.md.
Do not edit files your item does not name.
If a tripwire fires, stop and report instead of working around it.
Prefer reporting a blocker over inventing a workaround.
```

## Redirecting mid-flight

- Agents run in the background; **`SendMessage` to the agent's id/name steers it without
  restarting**. Cheaper than killing and relaunching, and it keeps context.
- Redirect on the *first* wrong assumption, not the first wrong edit. The expensive failure
  mode is a plausible wrong premise carried for many steps.
- When redirecting, give the correction **and** the evidence (`FACTS.md` row, or file:line).
  A bare "that's wrong" tends to produce a different wrong answer.
- If an agent reports success without G3 evidence, treat it as **not done** and ask for the
  command output. This session produced a build that looked correct, had the right row count,
  and was silently corrupt.

## Cost-of-error notes

Rough ordering of how expensive each item is to get wrong:

- **High:** items 02, 08, 09 — silent data corruption, OOM at scale, irreplaceable data loss.
- **Medium:** items 01, 03, 04, 10 — wrong behaviour on untested volumes, or claims that
  propagate into docs and get quoted later.
- **Low:** items 05, 06, 07, 11 — documentation accuracy; wrong but self-contained.

Items 06 and 07 are low-risk to fix and high-value to get right, because incorrect figures in
docs have already propagated once (the 450GB → 450MB unit error reached three documents and
underpinned a headline claim).
