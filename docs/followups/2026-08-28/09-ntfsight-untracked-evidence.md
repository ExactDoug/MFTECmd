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

## Proposed fix

Commit them under a clearly-labelled evidence path, or gitignore them and archive elsewhere —
but decide deliberately. The current state is "important and unprotected".

## The 10 most likely mistakes, ranked

1. **Gitignoring them as "build output".** They are irreproducible measurements, not
   artifacts of a build.
2. **Committing without checking for machine-identifying content.** These are full filesystem
   listings of a personal workstation — paths, usernames, installed software. **Review before
   committing to a repo that may go public.**
3. **Assuming they can be regenerated.** They cannot; the volume no longer exists in that form.
4. **Committing only the CSVs and dropping the HTML report,** which holds the headline
   totals and duration.
5. **Rewriting the merged PR's citations to point at the new path** and calling that the fix —
   the file still needs to exist.
6. **Committing into `output/`,** a directory whose README implies it is disposable working
   output.
7. **Adding them to Git LFS** for ~250 KB, adding a dependency for no benefit.
8. **Assuming they are already covered by `.gitignore`.** `git check-ignore` returns nothing
   for them — verify rather than assume.
9. **Colliding with the peer session** active in this repo.
10. **Losing them by cleaning the working tree** (`git clean -fdx`) before deciding. That is
    the actual failure mode this item exists to prevent.
