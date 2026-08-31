# 02 — Dictionary encoding blocked by the Apache.Arrow file writer

**Repo:** mftecmd · **Severity:** known limitation (documented in README) · **Effort:** M–L · **Status:** open

## What is wrong

Dictionary-encoding `ParentPath` / `Extension` / `NameType` would shrink Arrow output by a
further ~30% on top of Zstd. It was implemented, measured, and **removed** because it
produced silently corrupt files.

## Evidence

`Apache.Arrow` 22.1.0's `ArrowFileWriter` writes only the dictionary present in the *first*
record batch and silently discards later, grown ones. Since an `$MFT` is streamed, new paths
keep appearing:

```
ParentPath  batch 0: dict_len=1047  max_idx=1046  OK
            batch 1: dict_len=1047  max_idx=1640  OUT OF BOUNDS
            batch 5: dict_len=1047  max_idx=4773  OUT OF BOUNDS
```

Measured size was genuinely better: 989,362 B vs 1,434,514 B (Zstd alone) vs 6,570,778 B
(uncompressed). The file had a valid schema and the correct 52,210 row count. It failed only
on value access: `pyarrow.lib.ArrowIndexError: Index 1047 out of bounds`.

The Arrow IPC **file** format permits only one non-delta dictionary batch per dictionary id.
Delta dictionaries exist in the spec and `isDelta`/`DeltaDictionary` symbols are present in
the assembly, but there is no public write path.

## Evidence caveat

The batch-bounds table above and the `989,362 B` figure were produced by an implementation that
was **never committed** — PR #3 carries only the Zstd commit. They are reproducible only by
re-implementing the change. Two auditors correctly flagged them as unverifiable from the repo.
The *mechanism*, by contrast, is verifiable at Apache source and in the resolved assembly (a
full-filesystem search finds only 22.1.0 cached, so no shadowing copy).

## Options

1. **Leave as-is.** Zstd already gives 4.58x. Lowest risk.
2. ~~**`ArrowStreamWriter`** — streams support delta dictionaries.~~ **FALSE LEAD, do not
   attempt.** Verified against Apache source at tag `v22.1.0`: `ArrowFileWriter : ArrowStreamWriter`,
   and it overrides 10 methods, **none** of them `WriteRecordBatchInternal`, `WriteDictionaries`,
   or anything touching `HasWrittenDictionaryBatch`. The gate
   (`if (!HasWrittenDictionaryBatch) { ...; HasWrittenDictionaryBatch = true; }`) and the
   hardcoded `CreateDictionaryBatch(..., false)` beside a `// TODO: Support delta.` both live in
   the **base class** you would be switching to. Switching produces byte-identical corruption,
   loses the file footer and random access, and breaks ntfsight's `ipc.open_file`. All cost, no
   benefit.
3. **Fixed dictionaries only.** `NameType` is bounded by the `NameTypes` enum, so it could be
   pre-seeded and never grow. Marginal gain under Zstd; verify before building.
4. **Upstream fix/issue** against apache/arrow for the silent-discard behaviour.

## The 10 most likely mistakes, ranked

1. **Trusting Arrow's `status.html`, which marks C# ✓ for "Delta dictionaries".** That ✓ is
   **read-side only** (added 2021 by a PR that touched no writer file). It is precisely what makes
   Option 2 look viable. No Apache.Arrow C# writer — file *or* stream — can emit a second or delta
   dictionary batch in 22.1.0, and 23.0.0 carries the same `// TODO: Support delta.`
2. **Validating by row count and file size only.** That is exactly what the corrupt build
   passed. You must read every value back and compare against an uncompressed baseline.
2. **Assuming a newer Apache.Arrow fixed it** without re-running the batch-bounds check.
   Verify against the pinned version actually in `MFTECmd.csproj`.
3. **Testing on a fixture small enough to fit one batch.** `DefaultBatchSize` is 10,000;
   `xw` (638 rows) and `NIST` (117) produce a single batch and will *never* reproduce the bug.
   Use `tdungan` (52,210 rows, 6 batches).
4. **Concluding "it works" from a pyarrow read that never touches the strings.** `read_all()`
   can succeed; the error surfaces on `.to_pandas()` or a cast.
5. **Quoting the whole-table dictionary savings as achievable.** Those were measured with a
   single combined dictionary. Per-batch dictionaries at 10,000 rows save materially less.
6. **Dictionary-encoding `FileName`.** Cardinality is ~0.62 on tdungan and ~1.0 on the small
   fixtures, where it is a net *loss*.
7. **Raising `DefaultBatchSize` to "fix" it.** That reduces how often the bug triggers
   without removing it — the most dangerous possible outcome.
8. **Switching to `ArrowStreamWriter` without checking downstream.** `ntfsight`'s loader
   opens the file via `ipc.open_file`; a stream needs `open_stream`.
9. **Forgetting the schema change is breaking.** Dictionary-typed columns change the schema
   consumers see, even when values are identical.
10. **Re-litigating this from scratch** because the README says "not used" without reading
    why. The investigation is written up; start from it.
