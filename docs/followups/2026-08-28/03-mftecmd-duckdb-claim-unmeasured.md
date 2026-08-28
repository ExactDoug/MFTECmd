# 03 — The `~1600x` DuckDB claim was never measured

**Repo:** mftecmd · **Severity:** unsubstantiated claim · **Effort:** M · **Status:** open

## What is wrong

Commit `e3673e5`'s body states:

> Arrow output provides ~1600x faster loading into analytical databases like DuckDB
> compared to CSV parsing.

No measurement exists anywhere in the repo. An exhaustive search across all branches found
the figure **only** in that commit body; it never reached documentation. There is also still
**no DuckDB code in mftecmd** — no package reference, no loader, no script.

## What we do know (from ntfsight, not mftecmd)

On a real 2.37M-record volume: Arrow → DuckDB load took **680 ms (~3.48M records/sec)**,
versus a full PowerShell CSV pipeline at ~111 minutes. Those are not comparable operations —
one is a load, the other an end-to-end analysis — so they do not substantiate "1600x" either.

## Proposed fix

Either measure it honestly or drop the claim. A defensible comparison is narrow:
*time to get N rows queryable in DuckDB*, Arrow IPC vs `read_csv_auto`, same machine, same
data, Release build, ≥3 runs, report the spread.

## The 10 most likely mistakes, ranked

1. **Comparing unlike operations.** Zero-copy Arrow *registration* vs full CSV *parse +
   type inference + table materialisation* is not a like-for-like load. Say which you measured.
2. **Benchmarking with the file in page cache** after just writing it, so the Arrow read is
   warm and the CSV read is not (or vice versa).
3. **Measuring the wrong CSV.** The `--flo` CSV (6 cols, ~191 B/row) and the full `--csv --at`
   (34 cols, ~715 B/row) differ ~4x. State which, and prefer comparing equivalent columns.
4. **Counting Zstd decompression as free.** Arrow output is now compressed by default; that
   cost belongs in the load measurement.
5. **Using a Debug build.** Every timing in this repo's history is Debug and therefore not a
   benchmark.
6. **Running on the 52K-row fixture and extrapolating 45x to 2.4M rows.** Load characteristics
   are not linear; fixed costs dominate at small N.
7. **Forgetting `read_all()` materialises the whole table.** If the pipeline does that, the
   "zero-copy" framing is misleading — measure what the code actually does.
8. **Timing from Python with pyarrow when the shipped consumer is C#/.NET.** Pick the runtime
   the claim is about.
9. **Reporting a single run.** With a 9p/`\\wsl$` boundary in play, variance is large.
10. **Quietly leaving the claim in the git history** after measuring something different.
    If the number changes, say so where a reader will encounter it.
