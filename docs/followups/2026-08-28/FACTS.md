# Verified facts ledger

Every number below was measured or read from a primary artifact. The **"does NOT mean"** column
exists because most errors in this work came from correctly-quoted numbers applied to the wrong
thing.

**If a number is not in this file, treat it as unverified.**

> **Revised 2026-08-30 after an adversarial audit found nine defects in the first version**,
> including a citation that did not contain its own number. Provenance is now explicit about
> which figures are repo-verifiable and which are memory-sourced.

## Provenance classes

| Class | Meaning |
|---|---|
| **repo** | In a committed file. Anyone can check it |
| **untracked** | In a real file on this machine that is **not in git** (see item 09) |
| **memory** | claude-mem observation only. Not in any repo. **Unreproducible** |
| **derived** | Arithmetic from the above. Show your working |

## Real-volume measurements (2025-12-27 C: drive)

| Quantity | Value | Class / source | Does NOT mean |
|---|---|---|---|
| Volume | 592.51 GB, 2,377,605 files | untracked: `ntfsight/output/quickstart/DiskUsageReport.html:92-96` | Not today's C:, which is 270 GB |
| Python-pass entries | 2,366,478 (1,841,856 files + 524,622 dirs) | untracked: `ntfsight/src/python/test-output/test_report.json:4-12` | A *different pass* from the 2,377,605 figure. Not interchangeable |
| `$MFT` size | 2,313,682,944 B (2.1548 GiB) | untracked: `ntfsight/src/python/test-output/test_top_files.csv:18` | ~2,259,456 slots, not 2.26M files |
| Full CSV (`--csv --at`) | 1.7 GB | repo: `ntfsight/README.md:109` | Circular if used to derive its own B/record. Not the `--flo` output |
| `--flo` CSV | 453,673,692 / 453,276,005 / 453,272,933 B (~453 MB) | untracked: `test_top_files.csv:57-60` (3 distinct values across 4 lines) | 190.8 B/record. This is what the "450GB" typo meant |
| Arrow IPC output | 406 MB / 2,366,478 records | **memory**: obs #1053 | **Uncompressed**; Zstd shipped later. Not in either repo |
| Arrow to DuckDB load | 680 ms (~3.48M rec/sec) | **memory**: obs #1053 | **NOT** commit `d0665cb` - that commit does not contain the number. No CSV load was ever timed |
| Full Python pipeline | 4.037 s | **memory**: obs #1053 | Whole pipeline, not a load |
| PowerShell full pass | 6,666.23 s (1 h 51 m) | untracked: `DiskUsageReport.html:96` | 356.66 rec/s against 2,377,605; 355.00 against 2,366,478. State the denominator |
| Peak RAM | 17+ GB then ~2 GB | repo: ntfsight commit `bb2d316` | PowerShell path only; Python path untested |

**Derived:** `6,666.23 / 4.037 = 1651x` - the likely origin of commit `e3673e5`'s "~1600x".
Whole-pipeline ratio, not a load comparison. See item 03.

## Fixture measurements (git-tracked, reproducible)

| Fixture | `$MFT` bytes | Slots | In-use | Free | Other slots | Arrow rows |
|---|---|---|---|---|---|---|
| `tdungan` | 53,575,680 | 52,320 | 42,861 | 9,459 | 0 | 52,210 |
| `xw` | 643,072 | 628 | 467 | 159 | **2 `BAAD`** | 638 |
| `NIST/DFR-16` | 157,696 | 154 | 117 | 0 | **37 all-zero** | 117 |

Path: `mft/MFT.Test/TestFiles/<name>/$MFT`. **No admin needed.**

- `tdungan` reconciles exactly (42,861 + 9,459 = 52,320). `xw` does **not** (467 + 159 = 626, not
  628) because two slots are `BAAD`. `NIST` has 37 uninitialised slots.
- "All have `FILE` magic" is true of each file's **first 4 bytes** but **false at slot level** for
  `xw` and `NIST`.
- `rows/slot` is 0.998 for `tdungan` only. `xw` is **1.016** (rows exceed slots) and `NIST` is
  **0.76**. `rows = bytes/1024` is a tdungan-shaped rule of thumb, not a law.
- **A second, identical fixture set exists** in the standalone `~/dev/projects/github/mft` clone.
  Six `$MFT` files total. Make sure you know which repo you are reading.
- Depth from `~/dev` is **8 components** (9 for `NIST/DFR-16`). `find -maxdepth 4|5|6` find
  nothing; `-maxdepth 7` finds 2; `-maxdepth 8` finds 5; `-maxdepth 9` finds all 6.

## Arrow output, tdungan (52,210 rows), Debug build

| `--arrowc` | Bytes | B/row | vs none |
|---|---|---|---|
| `none` | 6,570,778 | 125.85 | 1.00x |
| `lz4` | 2,245,202 | 43.00 | 2.93x |
| `zstd` (default) | 1,434,514 | 27.48 | **4.58x** |

All three verified byte-identical in content across all 11 columns.

### Per-column cost, uncompressed

| Column | B/row | % of 125.85 |
|---|---|---|
| ParentPath | 55.82 | 44.4% |
| FileName | 17.66 | **14.0%** |
| NameType | 12.72 | 10.1% |
| FileSize / Created0x10 / LastModified0x10 | 8.00 each | 19.1% |
| Extension | 7.24 | 5.8% |
| EntryNumber / ParentEntryNumber | 4.00 each | 6.4% |
| IsDirectory / InUse | 0.125 each | 0.2% |

The column values sum to **125.70**, not 125.85. The **0.15 B/row residual is IPC framing and
schema overhead**, not a column. Percentages are of 125.85 and total 100.0.

**Fixed structural cost = 48.375 B/row**, derived: 16.000 (four string offset buffers at 4 B) +
0.125 (Extension validity bitmap) + 24.000 (3 x 8 B) + 8.000 (2 x 4 B) + 0.250 (2 bool bitmaps).
The remaining 77.31 B/row is UTF-8 payload - which is why the real volume cost more per row than
this fixture.

## This machine, today (unprivileged `stat` on the metafile)

| Volume | `$MFT` | Slots | Counted entries |
|---|---|---|---|
| C: 269.5 GiB | 2,882,011,136 B (2.684 GiB) | 2,814,464 | 2,662,489 (floor; ~250 dirs denied) |
| Y: 50 GB | 2,883,584 B | 2,816 | ~2,657-2,682 |
| W: 320 GB | 65,536 B | 64 | 8 |

Planning figure: **~2.7M in-use records in a 2.88 GB `$MFT`.** Slot counts assume 1,024-byte
records; for C: that is *measured* (max observed record 2,790,039 implies a 1,033 B ceiling), not
assumed. See item 11 for the large-FRS case that breaks it.

**Today's C: is the same filesystem as December's**, not a replacement: `$MFT`, `$MFTMirr`,
`$Boot` and `$AttrDef` all carry format timestamp `2024-07-25 01:25:42.429573300`, matching the
December record. It was **shrunk** (592 GB to 270 GB) while its `$MFT` **grew** 2.15 to 2.68 GiB -
a same-volume proof that `$MFT` never shrinks. Earlier versions of this ledger said the volume
"no longer exists"; that was wrong. The *data* is still unreproducible.

## Timings that are NOT benchmarks

Debug build, over the `\\wsl$` 9p boundary, tdungan: full+arrow 21.0875 s, `--flo --arrow`
18.5295 s, `--flo --csv+arrow` 18.5853 s, `--flo --csv` 18.99 s. **Class: memory** (obs
#83755/#83759/#83761). Only 21.1 / 18.5 appear in PR #2, rounded.

The repo's real benchmark is `README.md:181-186` - three rows, from a 2.1 GB / ~2.15M-record
volume: `--fl` **~133 s (no build label)**, `--flo` Debug **~87 s (35%)**, `--flo` Release
**~80 s (40%)**. The prose at `:176` says **~38%**. "38-40%" appears nowhere. Because the baseline
is unlabelled, this is **not** a demonstrated Release-vs-Release comparison.

## Citation structure

| Report | Citation **sites** | Inline **markers** | Distinct IDs | Numbered | Supporting |
|---|---|---|---|---|---|
| Apache Arrow | 115 | 225 | 52 | 34 | 18 (all have URLs) |
| DuckDB | 160 | 297 | 58 | 39 | 19 (**no URLs** - item 10) |

A *site* is one contiguous marker run in the source; a *marker* is one emitted `](#ref-...)` link.
The first version of this table labelled 115/160 as "markers".

`.docx` export citation sites carry **exactly one link each**; grouped citations were collapsed
(DuckDB worst case: 10 sources to 1). The export is not a citation source of truth.
