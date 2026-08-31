# 14 — The `--drive` CLI feeds an Arrow file to the CSV loader

**Repo:** ntfsight · **Severity:** likely broken path · **Effort:** S · **Status:** open · **Found:** 2026-08-30 audit

## What is wrong

`src/python/ntfsight/__main__.py`:

```python
csv_path = loader.extract_mft(args.drive)   # :111 - returns mft_data.arrow (Arrow IPC)
...
db = loader.load_csv_to_duckdb(csv_path)    # :123 - feeds it to read_csv_auto()
```

`extract_mft` invokes MFTECmd with `--arrow` (`mft_loader.py:122-126`) and returns an **Arrow IPC**
path. `load_csv_to_duckdb` passes it to DuckDB's `read_csv_auto()`. The variable name `csv_path`
is what makes it read as correct.

Consequence: the `--drive` CLI path — the primary documented entry point — never reaches
`load_arrow_to_duckdb` at `:187`, and almost certainly fails or produces garbage on a binary file.

## Relationship to item 08

Item 08 covers the unbounded `read_all()` at `:187`. This is a *different* defect: that line is
not reached by the CLI at all. Item 08 still stands, because `:187` is reached from
`test_full_pipeline.py:131`, `test_profiler.py:26`, and the documented public API
(`src/python/README.md:22`), and because the legacy CSV path at `:242-252` is equally unbounded.

## Verification

Cannot be run end-to-end here: `extract_mft` needs Administrator to read a live `C:\$MFT`, which
is unavailable on this machine. Confirm by inspection, or by calling `load_csv_to_duckdb` directly
against an existing `.arrow` file.

## The 10 most likely mistakes, ranked

1. **Assuming it works because the code reads plausibly.** The `csv_path` variable name is the
   whole problem.
2. **Trying to reproduce end-to-end.** `--drive` needs admin; you do not have it. Test the loader
   call directly.
3. **Treating this as a duplicate of item 08.** Different line, different defect; both are real.
4. **Fixing by renaming the variable** without changing which loader is called.
5. **Switching `:123` to `load_arrow_to_duckdb` and stopping there** — that inherits item 08's
   unbounded `read_all()` on a whole-volume input.
6. **Assuming `read_csv_auto` will error loudly.** It may partially parse binary and produce a
   populated-but-wrong table.
7. **Not checking whether `extract_mft`'s return contract is documented** elsewhere as CSV.
8. **Missing that `--csv` is a separate CLI input** that legitimately routes to the CSV loader.
9. **Editing on a stale branch.** ntfsight `master` moved through PR #5; fetch first.
10. **Colliding with the peer session** active in that repo.
