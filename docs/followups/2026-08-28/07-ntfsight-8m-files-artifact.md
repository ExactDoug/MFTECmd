# 07 — The "8.5M+ files" figure is a counting artifact

**Repo:** ntfsight · **Severity:** wrong figure · **Effort:** S · **Status:** open

## What is wrong

`CHANGELOG.md:12` and `README.md:305` cite **8.5M+ files**. The largest volume this tooling
was ever actually run against held **2,377,605** entries (PowerShell pass) / **2,366,478**
(Python pass).

The figure is arithmetically inconsistent with its own neighbours: the *same* 1.7 GB CSV is
attributed to 2.3M files at `README.md:109`. 1.7 GB ÷ 8.5M = ~200 B/record, which is `--flo`
density, not full-CSV density (~715 B/record).

Most likely origin: a pre-fix line count of the same C: drive, before commit `2979482`
stopped DOS 8.3 names double-counting every row, and before `--sn` was dropped. ~2.3M real
files counted ~2x, plus directory and ADS rows.

## Also affected

`docs/design/environment-setup-requirements.md:856` — "Benchmark with 8.5M file dataset" is a
*plan*, not a claim. It can stay, but is worth re-scoping to a number that reflects reality.

## Proposed fix

Correct `CHANGELOG.md:12` and `README.md:305` to the measured ~2.4M, noting the memory result
(17+ GB → ~2 GB) still holds — only the file count was wrong. Keep the aspirational 5M/8M
targets clearly labelled as untested.

## The 10 most likely mistakes, ranked

1. **Editing the CHANGELOG entry as if it were a fresh claim.** It is a historical release
   note; correct the number but preserve that it describes a past change.
2. **Also "correcting" `README.md:109`'s 2.3M figure,** which is the *right* one.
3. **Assuming 8.5M was fabricated.** It was a real count of a real, double-counted output.
   Say "double-counted", not "wrong".
4. **Deleting the memory claim along with the file count.** 17+ GB → ~2 GB is measured and
   independently corroborated by commit `bb2d316`.
5. **Missing that `README.md:110`'s whole 1 TB / 5-8M row is extrapolation,** not measurement —
   only the row above it has a matching artifact on disk.
6. **Changing the untested capacity ladders** in `tests/README.md` and the tech reference.
   Those are targets; leave them, but do not cite them as achieved.
7. **Using 2,377,605 and 2,366,478 interchangeably.** They are two different passes over the
   same volume; pick one and say which.
8. **Not cross-checking against the artifact.** `output/quickstart/DiskUsageReport.html:92-96`
   holds the authoritative figures — and is currently untracked (item 09).
9. **Committing to the retired `docs/consolidate-documentation` branch.**
10. **Colliding with the peer session** working in this repo. Fetch first.
