# 09 — Cited evidence artifacts are untracked and at risk

**Repo:** ntfsight · **Severity:** data loss risk · **Effort:** S · **Status:** open

## What is wrong

`git status` at ntfsight `master` = `03ebea5`:

```
?? output/quickstart/
?? src/python/test-output/
```

Neither is gitignored — they were simply never committed. They contain the **only surviving
record** of the 2025-12-27 run against a 592.51 GB / 2,377,605-file volume, on a machine that
has since been re-partitioned. That volume cannot be reproduced.

## Why it matters

A merged PR cites these files as its evidence base. `src/python/test-output/test_top_files.csv`
lines 57-60 hold the three reproducible ~453 MB FileListing measurements that disproved the
"450GB" claim, and line 18 records the real `$MFT` at 2,313,682,944 bytes. A reviewer
following that citation today finds nothing.

Contents (~250 KB total):
```
output/quickstart/DiskUsageReport.html   17,655 B   the 2,377,605-file / 592.51 GB report
src/python/test-output/test_report.json  55,702 B   2,366,478 entries, tier split
src/python/test-output/test_report.html  41,887 B
src/python/test-output/test_summary.csv  55,995 B
src/python/test-output/test_top_files.csv 93,056 B  the $MFT and CSV size evidence
```

## Severity escalation (2026-08-30 audit)

These files are **not merely personal paths**. A characterisation pass found they expose the
Windows account name, employer/tenant identity via OneDrive-for-Business paths (185 of 499 rows),
**named third-party client organisations** as folder names (including directories titled with
security/incident-response document names), and **5 unique individuals' email addresses** at those
client domains, embedded in mailbox-archive filenames.

`ExactDoug/ntfsight` is a **public** repository. Committing these as-is is a **client-data
disclosure**, not a personal-privacy concern. Redaction or anonymisation is required, not review.

**They are already cited from tracked files on `master`:**
- `docs/research/mft-analysis-optimization.md:985`
- `docs/planning/development-roadmap.md:401`
- merged PR #3's body (citation already dangling)

## Proposed fix

Commit them under a clearly-labelled evidence path, or gitignore them and archive elsewhere —
but decide deliberately. The current state is "important and unprotected".

## The 10 most likely mistakes, ranked

1. **Gitignoring them as "build output".** They are irreproducible measurements, not
   artifacts of a build.
2. **Committing raw.** This is a **public** repo containing named client organisations and
   individuals' email addresses. Redact or relocate; reviewing is not sufficient.
3. **Assuming they can be regenerated.** They cannot. It is the *same filesystem* (all four NTFS
   metafiles share format timestamp `2024-07-25 01:25:42.429573300`) but **shrunk** 592 GB to
   270 GB, `$MFT` grown 2.15 to 2.68 GiB. Same volume, unrecoverable state.
4. **Committing only the CSVs and dropping the HTML report,** which holds the headline
   totals and duration.
5. **Repointing citations** and calling that the fix. Three dangle (two tracked docs plus merged
   PR #3), and the files still need to exist somewhere.
6. **Committing into `output/`,** a directory whose README implies it is disposable working
   output.
7. **Adding them to Git LFS** for ~250 KB, adding a dependency for no benefit.
8. **Assuming they are already covered by `.gitignore`.** `git check-ignore` returns nothing
   for them — verify rather than assume.
9. **Colliding with the peer session** active in this repo.
10. **Losing them by cleaning the working tree** (`git clean -fdx`) before deciding. That is
    the actual failure mode this item exists to prevent.
