# 11 — The unprivileged `$MFT` sizing technique is undocumented

**Repo:** tooling / both · **Severity:** missing capability doc · **Effort:** S · **Status:** open

## What this is

Reading `C:\$MFT` needs Administrator. **Sizing** it does not. WSL2's `drvfs` mount exposes
NTFS metafile metadata even though content reads are denied:

```bash
stat -c '%s' '/mnt/c/$MFT'   # 2882011136   -> instant, no elevation
ls -la '/mnt/c/$MFT'         # Permission denied
```

Measured on this machine:

| Volume | `$MFT` bytes | 1 KB slots |
|---|---|---|
| C: (269.5 GB) | 2,882,011,136 (2.68 GiB) | 2,814,464 |
| Y: (50 GB) | 2,883,584 | 2,816 |
| W: (320 GB) | 65,536 | 64 (NTFS minimum) |

Corroborated three ways: Y: enumerates completely at 95.2% slot utilisation; W: sits at
NTFS's minimum allocation; and on C:, sampling `st_ino` (whose low 48 bits *are* the MFT
record number) across 19,961 System32 files hit a max of 2,790,039 — 99.1% of the slot count
and never above it.

## Why it is worth documenting

It lets you size a target `$MFT`, predict output size and runtime, and sanity-check capacity
planning **before** committing to an elevated extraction. `fsutil fsinfo ntfsinfo` requires
elevation; this does not.

## Proposed fix

Add to the MFTECmd README (or a tooling note) alongside the Administrator requirement, with
the caveat below.

## The 10 most likely mistakes, ranked

1. **Believing this grants read access.** It does not. `stat` works; `open()` fails. It is
   metadata only.
2. **Assuming `$MFT` size scales with volume size.** It scales with file count and churn
   history, and it **never shrinks**. This machine's *smaller* C: has a *larger* `$MFT` than
   the 592 GB volume scanned in December.
3. **Quoting the 12.5% "MFT zone" as an estimator.** That is reserved free space for
   anti-fragmentation, not the `$MFT`'s size, and Microsoft documents the exact ratios as
   undocumented and subject to change.
4. **Assuming slots == files.** Slots include free/deleted records; tdungan was 42,861 in-use
   of 52,320 slots (81.9%).
5. **Assuming rows == slots on every volume.** It held at 0.998 for tdungan, but the NIST
   fixture has 37 all-zero slots (0.76 rows/slot).
6. **Trying `fsutil fsinfo ntfsinfo` first** and concluding sizing is impossible when it
   fails on elevation.
7. **Using `powershell.exe` instead of `pwsh.exe`** from WSL and hitting the
   `CouldNotAutoloadMatchingModule` failure, unrelated to this technique.
8. **Assuming it works for any volume.** It needs a drive letter and a `drvfs` mount; the
   WINRE partition has neither and cannot be measured this way.
9. **Extrapolating output size with the fixture's ~126 B/row.** Real volumes ran ~172-180 B/row
   because `ParentPath` dominates and is path-shape dependent.
10. **Assuming `du`/`df` see it.** `$MFT` is not a normal file; only `stat` on the metafile
    path returns the size.
