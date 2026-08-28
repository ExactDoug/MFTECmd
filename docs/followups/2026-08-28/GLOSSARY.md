# Glossary — terms that have caused real errors here

Each entry below was actually confused during the originating session, producing a wrong
statement that had to be corrected. Precision in these words is not pedantry.

## Counting an `$MFT`

| Term | Meaning | Typical value (tdungan) |
|---|---|---|
| **Slot** / record slot | One `AllocatedRecordSize` division of the `$MFT` file. `file_size / 1024` on a normal volume. | 52,320 |
| **In-use FILE record** | A slot whose in-use bit is set. What MFTECmd's log line reports. | 42,861 |
| **Free record** | An allocated-but-deleted slot. **MFTECmd writes these to output too.** | 9,459 |
| **Output row** | One row in the CSV/Arrow. ≈ slots, not ≈ in-use records. | 52,210 |
| **File** | A logical file on the volume. Not equal to any of the above. | — |

**The error this caused:** "52,210 rows from 42,861 records — a 1.22x multiplier." There is
no multiplier. 42,861 in-use + 9,459 free = 52,320 slots, and rows/slot is **0.998**. The log
line reports in-use only; free records are written as well.

Rule of thumb: `rows ≈ $MFT_bytes / 1024`. Do not derive rows from a file count.

## Rows vs files

Rows can exceed files via alternate data streams (`$BadClus:$Bad`, `:Zone.Identifier`) — only
0.05% on tdungan — and, if `--sn` is passed, via separate DOS 8.3 name attributes, which can
roughly **double** row count on volumes with 8.3 generation enabled (item 05).

## Arrow sizes

| Term | Meaning | Trap |
|---|---|---|
| **On-disk / IPC size** | Bytes of the `.arrow` file | Affected by `--arrowc`; 4.58x spread |
| **In-memory size** | `Table.nbytes` | **Unchanged by compression.** `read_all()` decompresses (item 08) |
| **`get_total_buffer_size()`** | Allocated buffers incl. 8-byte padding | Slightly larger than `nbytes` |

"Zstd made it smaller" is true of the file and false of the memory footprint.

## Citations

| Term | Meaning |
|---|---|
| **turn ID** (`turn20search0`) | The research object's internal source id. **Namespaced per report** |
| **Panel number** (`[1]`-`[39]`) | The number shown in the report's Sources panel |
| **Primary / numbered** | A source with its own card in the panel |
| **Supporting** | A source attached to a grouped citation that never got its own card |
| **Grouped citation** | One inline marker carrying several sources (up to 10 seen) |

**The error this caused:** assuming turn IDs are global. `turn15search1` is
*Microsoft.Data.Analysis* in the Arrow report and *DuckDB CMake static targets* in the DuckDB
report. Resolving one report's IDs with the other's bibliography silently mis-cites everything.

Counts: Arrow 34 primary + 18 supporting = 52. DuckDB 39 + 19 = 58.

## Builds

| Term | Meaning |
|---|---|
| **Benchmark** | Release build, no 9p boundary, ≥3 runs, stated fixture |
| **Timing** | Anything else — including every timing this session produced |

Only the README's 38-40% figure qualifies as a benchmark, and its volume no longer exists.

## Paths

| Term | Meaning |
|---|---|
| **ext4 path** | `/home/dmortensen/...` — native WSL2 disk. Fast |
| **`\\wsl$\...`** | How Windows reaches ext4, via 9p. Same files, slow, zone-sensitive |
| **`winpath`** | Emits the local-zone `\\wsl$\` alias. Use for **arguments** to Windows tools |

**The error this caused:** describing the build as "over a `\\wsl$` share" implied slow
storage. The storage is fast ext4; what is slow is the Windows process crossing 9p for I/O.
The files were never on a network share.
