# 08 — `mft_loader.py` loads unbounded and cannot time out

**Repo:** ntfsight · **Severity:** scaling risk · **Effort:** M · **Status:** open

## What is wrong

`src/python/ntfsight/mft_loader.py` (verified at ntfsight `master` = `03ebea5`):

- **`:187` — `table = reader.read_all()`** materialises the entire Arrow file into RAM in one
  shot. No batch iteration, no row cap.
- **`:180`, `:236` — `duckdb.connect(":memory:")`** with no `memory_limit` pragma.
- **`:132` — `subprocess.run(...)`** invoking MFTECmd with **no `timeout=`**. A hung MFTECmd
  hangs the process indefinitely.

## Scale context

At the measured 406 MB / 2.37M records this is survivable (~400 MB resident before DuckDB does
anything). The declared design envelope is 2-3M entries; the *untested* ladder goes to 5M.
Today's C: on the dev machine already holds ~2.81M slots.

Zstd compression reduces the **file**, not the in-memory table — `read_all()` decompresses.
So compression does not mitigate this.

## Proposed fix

Iterate record batches instead of `read_all()`; register batches with DuckDB incrementally or
set an explicit `memory_limit`; add a `timeout=` to the subprocess call with a clear error.

## The 10 most likely mistakes, ranked

1. **Assuming Zstd fixed the memory problem.** It shrinks the file on disk; the in-memory
   Arrow table is unchanged. `nbytes` ≈ on-disk size only for the *uncompressed* format.
2. **Testing with a fixture and declaring it bounded.** 638 rows proves nothing; the risk is
   at 2-5M.
3. **Setting a `memory_limit` so low DuckDB spills constantly,** trading a crash for a
   silent 10x slowdown.
4. **Adding a subprocess timeout short enough to kill legitimate runs.** The real volume took
   minutes for extraction alone; the PowerShell pass took 111 minutes end-to-end.
5. **Switching to `open_stream` when the writer emits the file format.** MFTECmd uses
   `ArrowFileWriter`; the reader must match.
6. **Breaking the zero-copy property** by converting to pandas while "fixing" memory, which
   is strictly worse.
7. **Assuming `total_entries` == files.** The report's own numbers split 1,841,856 files +
   524,622 directories.
8. **Not handling `check=True` raising before a timeout fires** — order the error handling.
9. **Testing only the Arrow path.** There is a legacy CSV path at `:242-252` with the same
   unbounded shape.
10. **Ignoring that the `$MFT` extraction step needs Administrator,** so an end-to-end test
    cannot run unprivileged on this machine at all.
