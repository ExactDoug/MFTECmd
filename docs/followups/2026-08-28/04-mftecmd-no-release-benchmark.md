# 04 — No Release-build benchmark exists

**Repo:** mftecmd · **Severity:** missing evidence · **Effort:** S–M · **Status:** open

## What is wrong

Every timing produced during the Arrow work was a **Debug** build run over the `\\wsl$` 9p
boundary against a 52,210-row fixture:

```
full path + arrow   21.09 s
--flo --arrow       18.53 s
--flo --csv+arrow   18.59 s
--flo --csv         18.99 s
```

These were explicitly labelled "not a benchmark" in PR #2 and should never be quoted as one.

The README's real benchmark — `--fl` ~133 s vs `--flo` ~80 s Release, **38-40% faster** — comes
from a 2.1 GB / ~2.15M-record volume that **no longer exists in that form**. It has not been
re-validated since `--arrow` and Zstd landed.

## Proposed fix

Release build, input and output staged on `/mnt/c` so no 9p crossing is in the loop, ≥3 runs
each, report median and spread:
`--fl` · `--flo` · `--flo --arrow` · `--flo --arrow --arrowc none`

Best available target is `tdungan` (51 MB, 52,210 rows). It gives a defensible *relative*
figure; it cannot reproduce the README's absolute numbers.

## The 10 most likely mistakes, ranked

1. **Overwriting the README's 38-40% figure with fixture numbers.** Different volume, 45x
   smaller. Add a second row; do not replace.
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
