# Verified facts ledger

Every number below was measured or read from a primary artifact during the originating
session. The **"does not mean"** column exists because most errors in this work came from
correctly-quoted numbers applied to the wrong thing.

**If a number is not in this file, treat it as unverified.**

## Real-volume measurements (2025-12-27, C: drive, since re-partitioned)

| Quantity | Value | Source | Does NOT mean |
|---|---|---|---|
| Volume | 592.51 GB, 2,377,605 files | `output/quickstart/DiskUsageReport.html:92-96` (untracked) | Not today's C: — that machine was re-partitioned |
| Python-pass entries | 2,366,478 (1,841,856 files + 524,622 dirs) | `src/python/test-output/test_report.json:3-10` | Different pass than the 2,377,605 figure; not interchangeable |
| `$MFT` size | 2,313,682,944 B (2.15 GiB) | `test_top_files.csv:18` | ~2.26M slots, not 2.26M files |
| Full CSV (`--csv --at`) | 1.7 GB, ~715 B/record | ntfsight `README.md:109` | Not the `--flo` output |
| `--flo` FileListing CSV | ~453 MB, ~191 B/record, reproduced 3x | `test_top_files.csv:57-60` | This is the number the "450GB" typo meant |
| Arrow IPC output | 406 MB / 2,366,478 records (~172-180 B/row) | ntfsight obs #1053 | **Uncompressed.** Zstd shipped later |
| Arrow → DuckDB load | 680 ms (~3.48M rec/sec) | commit `d0665cb` | A load, not an end-to-end pipeline |
| PowerShell full pass | 6,666.23 s (1 h 51 m) | DiskUsageReport.html:96 | ~356 rec/s actual, not the README's ~700 |
| Peak RAM (pre-fix) | 17+ GB → ~2 GB after streaming | commit `bb2d316` | PowerShell path only; Python path untested |

## Fixture measurements (git-tracked, reproducible)

| Fixture | `$MFT` bytes | Slots | In-use | Arrow rows |
|---|---|---|---|---|
| `tdungan` | 53,575,680 | 52,320 | 42,861 (+9,459 free) | 52,210 |
| `xw` | 643,072 | 628 | 467 (+159 free) | 638 |
| `NIST/DFR-16` | 157,696 | 154 | 117 | 117 |

Path: `mft/MFT.Test/TestFiles/<name>/$MFT`. All have `FILE` magic. **No admin needed.**
They sit 5 levels below `~/dev`, so `find -maxdepth 4` misses them.

## Arrow output, tdungan (52,210 rows), Debug build

| `--arrowc` | Bytes | B/row | vs none |
|---|---|---|---|
| `none` | 6,570,778 | 125.85 | 1.00x |
| `lz4` | 2,245,202 | 43.00 | 2.93x |
| `zstd` (default) | 1,434,514 | 27.48 | **4.58x** |

All three verified byte-identical in content across all 11 columns.

### Per-column cost, uncompressed

| Column | B/row | % |
|---|---|---|
| ParentPath | 55.82 | 44.4% |
| FileName | 17.66 | 14.1% |
| NameType | 12.72 | 10.1% |
| FileSize / Created / LastModified | 8.00 each | 19.1% |
| Extension | 7.24 | 5.8% |
| EntryNumber / ParentEntryNumber | 4.00 each | 6.4% |
| IsDirectory / InUse | 0.13 each | 0.2% |

Fixed structural cost is **~48.4 B/row**; the rest is UTF-8 payload. This is why the real
volume cost ~172-180 B/row against the fixture's 125.85 — deep `node_modules` paths.

## This machine, today (unprivileged `stat` on the metafile)

| Volume | `$MFT` | Slots | Counted entries |
|---|---|---|---|
| C: 269.5 GB | 2,882,011,136 B (2.68 GiB) | 2,814,464 | 2,662,489 (floor; ~250 dirs denied) |
| Y: 50 GB | 2,883,584 B | 2,816 | 2,682 |
| W: 320 GB | 65,536 B | 64 | 8 |

Planning figure: **~2.7M in-use records in a 2.88 GB `$MFT`.**

## Timings that are NOT benchmarks

Debug build, over the `\\wsl$` 9p boundary, tdungan: full+arrow 21.09 s, `--flo --arrow`
18.53 s, `--flo --csv+arrow` 18.59 s, `--flo --csv` 18.99 s.

The README's **38-40%** figure is the only real benchmark: Release build, 2.1 GB `$MFT`,
~2.15M records, `--fl` ~133 s vs `--flo` ~80 s. That volume no longer exists.

## Citation structure

| Report | Markers | Distinct IDs | Numbered | Supporting |
|---|---|---|---|---|
| Apache Arrow | 115 | 52 | 34 | 18 (all have URLs) |
| DuckDB | 160 | 58 | 39 | 19 (**no URLs** — item 10) |

`.docx` export citation sites carry **exactly one link each**; grouped citations were
collapsed (worst case 10 sources → 1). The export is not a citation source of truth.
