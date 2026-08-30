# 04 — No Release-build benchmark exists **for the Arrow work**

> **Corrected 2026-08-30 after adversarial audit.** The original title said no Release benchmark
> existed at all. `README.md:184` has carried one since 2025-11-25. Scoped correctly below.

**Repo:** mftecmd · **Severity:** missing evidence · **Effort:** S–M · **Status:** open

## What is wrong

Every timing produced during **the Arrow work** was a **Debug** build run over the `\\wsl$` 9p
boundary against a 52,210-row fixture:

```
full path + arrow   21.09 s
--flo --arrow       18.53 s
--flo --csv+arrow   18.59 s
--flo --csv         18.99 s
```

These were explicitly labelled "not a benchmark" in PR #2 and should never be quoted as one.

The repo's real benchmark is `README.md:181-186`, and it has **three** rows, not two:

```
| Standard `--fl`        | ~133 seconds | baseline     |   <- no build label
| `--flo` (Debug build)  | ~87 seconds  | 35% faster   |
| `--flo` (Release build)| ~80 seconds  | 40% faster   |
*Benchmark: 2.1GB $MFT with ~2.15 million file records*
```

Three caveats the original version got wrong:

- The table says **40%**; the prose at `README.md:176` says **~38%**. "38-40%" appears nowhere;
  the README is internally inconsistent and should be quoted as both.
- The `~133 s` baseline carries **no build label**, so this is not a demonstrated
  Release-vs-Release A/B. Given Debug-vs-Release on the same `--flo` workload is ~8% (87 vs 80),
  an unlabelled baseline is a real gap.
- Introduced by `8e5e864` on 2025-11-25 (PR #1). The volume behind it is the **same filesystem**
  as today's C: (all four NTFS metafiles share format timestamp `2024-07-25 01:25:42.429573300`),
  since **shrunk** 592 GB -> 270 GB with its `$MFT` grown 2.15 -> 2.68 GiB. Not re-created, but
  not reproducible either.

## Proposed fix

Release build, input and output staged on `/mnt/c` so no 9p crossing is in the loop, ≥3 runs
each, report median and spread:
`--fl` · `--flo` · `--flo --arrow` · `--flo --arrow --arrowc none`

Best available target is `tdungan` (51 MB, 52,210 rows). It gives a defensible *relative*
figure; it cannot reproduce the README's absolute numbers.

## The 10 most likely mistakes, ranked

1. **Overwriting the README's figures with fixture numbers.** Different volume, 45x smaller.
   Add rows; do not replace. And quote **40% (table) / ~38% (prose)**, not "38-40%".
2. **Leaving the source `$MFT` on ext4** while claiming the 9p boundary was removed. Both
   input *and* output must be Windows-side.
3. **Building Release but running the stale Debug `.exe`** left in `bin/Debug/net9.0/`.
4. **Forgetting `chmod +x` on the fresh Release binary,** then reporting a failed run.
5. **A single run per mode.** The Debug numbers varied ~10% run to run.
6. **Not stating the compression mode.** `--arrowc zstd` is now the default and costs CPU;
   an unlabelled `--flo --arrow` timing is ambiguous.
7. **Comparing against the old `--fl` path without confirming it still behaves the same**
   after the `IArrowRecord`/`FileListData` refactor.
8. **Benchmarking with an antivirus scanning the output directory** — a real effect on
   Windows for multi-hundred-MB writes.
9. **Reporting throughput as records/sec without saying which record count** — in-use
   (42,861), total slots (52,320), or output rows (52,210). All three differ.
10. **Treating first-run and warm-cache runs as interchangeable.** State which you report.
11. **Saying "every timing in this repo is Debug".** `README.md:184` is Release. That error
    appeared in the first version of this backlog and in item 03.
