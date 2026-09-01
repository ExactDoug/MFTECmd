# Follow-ups — 2026-08-28

Open items carried out of the Arrow/`--flo`/citation-repair work. Each was found and
verified during that session, then deliberately **not** acted on, either because it was
outside the agreed scope or because it needed a decision.

Nothing here is speculative. Every claim has a file:line or a measurement behind it.

## How to use these

Each item is self-contained: what is wrong, the evidence, a proposed fix, how to verify
the fix, and — most importantly — a ranked list of **the mistakes most likely to be made
while fixing it**.

That last section exists because these items are unusually trap-laden. Several look
trivial and are not. Read it before touching code.

**Do not trust this document over the repository.** Line numbers were correct at
`master` = `aab1c5c` (mftecmd) and `03ebea5` (ntfsight). Re-grep before editing.

## Index

| # | Item | Repo | Severity | Effort |
|---|------|------|----------|--------|
| [01](01-mftecmd-hardcoded-mft-record-size.md) | Hardcoded 1024-byte MFT record read | mftecmd | Latent bug | S |
| [02](02-mftecmd-arrow-dictionary-blocked-upstream.md) | Dictionary encoding blocked by Apache.Arrow writer | mftecmd | Known limitation | M–L |
| [03](03-mftecmd-duckdb-claim-unmeasured.md) | `~1600x` DuckDB claim is misattributed | mftecmd | Unsubstantiated claim | M |
| [04](04-mftecmd-no-release-benchmark.md) | No Release benchmark **for the Arrow work** | mftecmd | Missing evidence | S–M |
| [05](05-mftecmd-sn-row-count-doubling.md) | `--sn` raises row counts 1.02x-1.43x, undocumented | mftecmd | Doc gap / footgun | S |
| [06](06-ntfsight-arrow-export-target-unreachable.md) | "15-30 MB Arrow IPC" target is unreachable | ntfsight | Wrong target | S |
| [07](07-ntfsight-8m-files-artifact.md) | "8.5M+ files" figure is a counting artifact | ntfsight | Wrong figure | S |
| [08](08-ntfsight-mft-loader-unbounded.md) | `mft_loader.py` unbounded load, no timeout | ntfsight | Scaling risk | M |
| [09](09-ntfsight-untracked-evidence.md) | Cited evidence untracked **and contains client PII** | ntfsight | Data loss risk | S |
| [10](10-duckdb-supporting-refs-missing-urls.md) | 19 DuckDB supporting refs have no URL | both | Incomplete data | S |
| [11](11-tooling-unprivileged-mft-sizing.md) | Unprivileged `$MFT` sizing undocumented | tooling | Missing doc | S |
| [12](12-mftecmd-do-offset-semantics.md) | `--do` treats a documented byte offset as an entry index | mftecmd | Latent bug | S |
| [13](13-mft-hardcoded-fixup-stride.md) | Hardcoded 512-byte fixup stride | mft | Latent bug | S |
| [14](14-ntfsight-cli-arrow-into-csv-loader.md) | `--drive` CLI feeds an Arrow file to the CSV loader | ntfsight | Likely broken path | S |

## Progress log

| Date | Item | Status |
|---|---|---|
| 2026-08-30 | all | Adversarially audited; defects in all 11 items + 5 support files corrected |
| 2026-08-31 | **09** | **Done** - artifacts redacted and committed, raw archived outside the repo (ntfsight PR #7) |
| 2026-08-31 | **13** | **Fix pushed**, `mft` branch `fix/derive-fixup-stride` (`3ff0f57`); PR pending; submodule bump pending |
| 2026-08-31 | 01, 12 | Not started - depend on the item 13 submodule bump landing first |
| 2026-08-31 | 05, 06, 07 | **Blocked** - ntfsight PR #6 (peer session) rewrites `src/DiskUsageAnalyzer.ps1`, which is the evidence base for 05 and 07 |

### Item 13 addendum: three more sites

The item names `MFT/FileRecord.cs:84,102`. The same hardcoded 512 also appears at
`LogFile/LogPageRstr.cs:88,106` and `LogFile/LogPageRcrd.cs:71`. Those are **not fixed** - there is
no `$LogFile` fixture in `MFT.Test`, so the change could not be verified. Treat as a follow-up.

The shipped fix derives the stride from the record itself rather than from `$Boot` as this item
originally proposed: `rawBytes.Length / FixupData.FixupActual.Count`. The record is
self-describing, so no boot-sector lookup is needed. Verified to yield exactly 512 on all three
fixtures, i.e. behaviour on conventional volumes is unchanged.

## Audit provenance

Every item and support file in this directory was **adversarially audited on 2026-08-30** by six
independent read-only investigators instructed to disprove rather than confirm. They found
defects in all 11 items and all 5 support files, including four that would have actively
misdirected remediation. Those are corrected here, each marked with a `Corrected 2026-08-30` note
stating what the earlier version got wrong.

Findings that survived the audit are stronger for it; findings that did not were rewritten or
deleted. Where a claim rests on evidence that cannot be reproduced from the repo, it now says so.

## Reference docs — read before touching an item

| File | Purpose |
|------|---------|
| [GLOSSARY.md](GLOSSARY.md) | Terms that have caused real errors (slots vs records vs rows, turn IDs vs panel numbers, on-disk vs in-memory) |
| [FACTS.md](FACTS.md) | Every verified number, its provenance, and what it does **not** mean. Quote nothing outside this file |
| [TRAPS.md](TRAPS.md) | Cross-cutting environment and evidence traps |
| [VERIFY.md](VERIFY.md) | Copy-paste verification recipes |
| [CHECKPOINTS.md](CHECKPOINTS.md) | Gates, tripwires, divergence signals, agent briefing, redirect guidance |
| [verify.sh](verify.sh) | Runnable invariant checker — run before starting and before claiming done |

## Cross-cutting traps

These bit repeatedly during the originating session and will bite again:

1. **Build/run from WSL is three separate rules.** `winpath` for arguments, the *Linux*
   path to launch the `.exe`, and `chmod +x` after **every** build. Each fails differently.
   See [11](11-tooling-unprivileged-mft-sizing.md) and the `mftecmd-build-and-run-from-wsl` memory.
2. **Citation IDs are per-report namespaces.** `turn15search1` means different sources in
   the Arrow and DuckDB reports. Never map one report's IDs with another's bibliography.
3. **The `.docx` exports are lossy.** Every export citation site carries exactly one link;
   grouped citations were collapsed (one site went 10 sources → 1). Never treat the export
   as a source of truth for citations.
4. **Timings in this repo's history are not benchmarks** unless they say Release build.
5. **A file that parses is not a file that is correct.** The dictionary-encoding bug
   produced valid-looking Arrow files with the right row count that failed only on value
   access. Validate content, not structure.
