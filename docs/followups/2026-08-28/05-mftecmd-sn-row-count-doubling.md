# 05 — `--sn` can roughly double row counts, and this is undocumented

**Repo:** mftecmd · **Severity:** doc gap / footgun · **Effort:** S · **Status:** open

## What is wrong

`MFTECmd/Program.cs:2967` skips `NameTypes.Dos` FILE_NAME attributes unless `--sn` is passed.
On a volume that carries *separate* Dos and Win32 name attributes, enabling `--sn` can nearly
double output rows. Nothing in the README warns about this.

This is not theoretical: ntfsight hit it. Its commit `2979482` fixed DOS 8.3 entries
double-counting **everything by ~2x**, and `DiskUsageAnalyzer.ps1` now deliberately omits
`--sn` for that reason. The stale "8.5M files" figure (item 07) is a downstream symptom.

## Nuance that makes this confusing

The `tdungan` fixture is XP-era: 57% of its rows are `NameType=DosWindows`, a *combined*
attribute. So `--sn` barely changes row count there. A modern volume with separate Dos and
Win32 attributes behaves very differently. **The fixture will not show you this behaviour.**

## Proposed fix

Document it in the README next to `--sn` and `--flo`: state that `--sn` includes DOS 8.3 name
attributes, that this can approximately double row count on volumes with 8.3 generation
enabled, and that downstream consumers doing per-file aggregation should leave it off.

## The 10 most likely mistakes, ranked

1. **Testing on `tdungan` and concluding `--sn` is harmless.** Its combined `DosWindows`
   entries mask the effect entirely.
2. **Describing it as "adds short names" without saying rows roughly double.** The row-count
   consequence is the part that breaks consumers.
3. **Assuming 8.3 generation is always on.** It is commonly disabled
   (`fsutil behavior set disable8dot3 1`), so the multiplier is volume-dependent, not fixed.
4. **Calling it exactly 2x.** It is "up to roughly 2x", varying by how many files carry a
   separate DOS name.
5. **Conflating this with ADS rows.** Alternate data streams add rows too, but only ~0.05%
   on tdungan. Different mechanism, wildly different magnitude.
6. **Changing the default.** Upstream MFTECmd behaviour; do not silently alter it.
7. **Documenting only under `--sn` and not under `--flo`/`--fl`,** where people actually hit
   it while counting files.
8. **Assuming rows == files in the docs you write.** They are not equal even without `--sn`.
9. **Rewriting ntfsight's history** to say the 8.5M figure was "a bug" — it was a real count
   of a real (double-counted) output. Describe precisely.
10. **Adding a warning log on every run,** which is noise for the legitimate forensic use of
    `--sn`.
