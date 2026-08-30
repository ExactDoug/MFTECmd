# 05 — `--sn` materially increases row counts, and this is undocumented

**Repo:** mftecmd · **Severity:** doc gap / footgun · **Effort:** S · **Status:** open

> **Corrected 2026-08-30 after adversarial audit.** The previous version claimed `tdungan`
> masks this effect and that `--sn` "roughly doubles" rows. Both were false. Measured below.

## What is wrong

`MFTECmd/Program.cs:2966-2970` skips `NameTypes.Dos` FILE_NAME attributes unless `--sn`
(`includeShort`) is set. Enabling `--sn` therefore adds a row per separate DOS 8.3 name.
Nothing in the README warns that this changes row counts; `README.md:30` only says
*"sn  Include DOS file name types. Default is FALSE"*.

## Measured, not estimated

Parsed directly from the fixtures (FILE_NAME attribute types per base record):

| Fixture | rows w/o `--sn` | rows w/ `--sn` | ratio |
|---|---|---|---|
| `tdungan` | 52,185 | 74,396 | **1.426x** |
| `xw` | 628 | ~860-873 | ~1.37x |
| `NIST/DFR-16` | 117 | 119 | **1.02x** |

`tdungan` FILE_NAME breakdown: `DosWindows` 29,969 (combined, unaffected by `--sn`) ·
`Windows` 22,211 + `Dos` 22,211 (**separate pairs — these are what `--sn` adds**) · `Posix` 5.

So: **57% of tdungan's rows are combined names, but 43% are separate pairs.** The fixture
demonstrates the effect at +42.6% — it does **not** mask it. `NIST/DFR-16` (85% Posix) is the
fixture that masks it, at 1.02x.

The theoretical worst case on a volume where *every* long name carries a separate DOS name is
~2x. No fixture here reaches it; the observed range is **1.02x-1.43x**.

## Corroboration downstream

ntfsight hit this. Commit `2979482` — *"Fixes double-counting bug that inflated all statistics
by ~2x"* — and `951a142` (a separate commit) removed `--sn`, with
`src/DiskUsageAnalyzer.ps1:502-503` carrying the explanatory comment.

## Proposed fix

Document next to `--sn` and `--flo`: enabling `--sn` adds a row per separate DOS 8.3 name
attribute, observed at 1.02x-1.43x on available fixtures and up to ~2x in theory, and consumers
doing per-file aggregation should leave it off.

## The 10 most likely mistakes, ranked

1. **Repeating that `tdungan` masks the effect.** It shows it at +42.6%. Use `NIST/DFR-16` if
   you want the masking case.
2. **Writing "doubles".** Observed is 1.02x-1.43x. Say "up to ~2x in theory, 1.02x-1.43x measured".
3. **Assuming 8.3 generation is always on.** Commonly disabled via
   `fsutil behavior set disable8dot3 1`; the multiplier is volume-dependent.
4. **Conflating this with ADS rows.** ADS adds ~0.05% on tdungan — different mechanism, different
   magnitude by three orders.
5. **Changing the default.** Upstream MFTECmd behaviour; leave it.
6. **Documenting only under `--sn`,** not under `--flo`/`--fl` where people count files.
7. **Writing "rows == files" anywhere.** They are not equal even without `--sn`.
8. **Re-deriving the numbers with a parser that ignores ADS** and reporting a slightly different
   row count as a contradiction. The table above excludes ADS; MFTECmd's own totals include it
   (52,210 vs 52,185).
9. **Citing `2979482` as the commit that removed `--sn`.** That was `951a142`; `2979482` filtered
   Dos rows in the consumer.
10. **Adding a per-run warning log,** which is noise for the legitimate forensic use of `--sn`.
