# 03 — The `~1600x` DuckDB claim is misattributed

**Repo:** mftecmd · **Severity:** misattributed claim · **Effort:** M · **Status:** open

> **Corrected 2026-08-30 after adversarial audit.** This item previously said the figure "was
> never measured". That was wrong — and the item quoted both halves of the arithmetic on
> adjacent lines without performing the division. The defect is misattribution, not fabrication.

## What is wrong

Commit `e3673e5`'s body states:

> Arrow output provides ~1600x faster loading into analytical databases like DuckDB
> compared to CSV parsing.

The number is real:

```
6,666.23 s   PowerShell full pass   (ntfsight output/quickstart/DiskUsageReport.html:96)
÷    4.037 s Python+DuckDB+Arrow    (claude-mem ntfsight obs #1053)
=    1651x
```

`e3673e5` (mftecmd, 13:10:03 MST) and ntfsight `d0665cb` (13:10:16 MST) are **13 seconds apart** —
one session, one author, one number.

**The problem is what it measures.** That ratio compares two entire pipelines across two
languages and two query engines — PowerShell script vs Python + DuckDB + Arrow. The commit
credits it to Arrow-vs-CSV *loading*. The Arrow→DuckDB load alone was **680 ms**; a CSV load of
the same data was **never timed**, so the load-level claim the sentence makes has no
measurement behind it in either direction.

## Related, and also uncorrected

ntfsight carries a sibling figure the original version of this item failed to find:
`docs/planning/development-roadmap.md:94` — `| Processing time (2.5M) | 1h 52m | 30-60 seconds |
1,650x speedup |`. It is internally incoherent (6,720 ÷ 30-60 = 112-224x), sits under
"Performance **Targets**", and is marked **"Untested"** at `:500`. Commit `62fe278` corrected
the neighbouring `1000x+` row during the 450GB→450MB pass and skipped this one.

## Evidence quality

The `4.037 s` and `680 ms` figures exist **only in claude-mem obs #1053** — not in either repo.
`git log --all -p | grep 680` over ntfsight returns 0. So the derivation above is itself
one unreproducible number away from being as unsourced as the claim it corrects. Treat it as
"best available account", not proof.

Also still true: there is **no DuckDB code in mftecmd** — no package reference, no loader.
(`README.md:136-142` ships a 4-line Python reader snippet, and `docs/duckdb-windows-native-research.md`
is a research report; neither can produce a measurement.)

## Proposed fix

Either measure the narrow claim — *time to get N rows queryable in DuckDB*, Arrow IPC vs
`read_csv_auto`, same machine, same data, Release build, ≥3 runs — or restate the commit-message
claim as a whole-pipeline figure and say so.

## The 10 most likely mistakes, ranked

1. **Repeating "it was never measured".** It was. Saying so again is the error this correction
   exists to fix.
2. **Comparing unlike operations.** Zero-copy Arrow *registration* vs CSV *parse + type
   inference + materialisation* is not a like-for-like load. State which you measured.
3. **Treating obs #1053 as repo-verifiable.** It is a claude-mem observation; no file in either
   repo contains `4.037` or `680`.
4. **Measuring the wrong CSV.** `--flo` (~191 B/row) vs full `--csv --at` (~715 B/row) differ ~4x.
5. **Counting Zstd decompression as free.** Arrow output is compressed by default now.
6. **Using a Debug build.** (But do not say "every timing in this repo is Debug" — `README.md:184`
   is a Release figure. That claim was wrong here and is corrected in item 04.)
7. **Extrapolating from the 52K-row fixture** to 2.4M rows; fixed costs dominate at small N.
8. **Forgetting `read_all()` materialises the whole table** — measure what the code does, not the
   "zero-copy" framing.
9. **Timing from Python when the shipped consumer is C#/.NET.**
10. **Fixing this and leaving ntfsight's `1,650x` untouched** — same lineage, same defect.
