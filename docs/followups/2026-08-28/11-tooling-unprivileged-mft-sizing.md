# 11 — The unprivileged `$MFT` sizing technique is undocumented

**Repo:** tooling / both · **Severity:** missing capability doc · **Effort:** S · **Status:** open

## What this is

Reading `C:\$MFT` needs Administrator. **Sizing** it does not. WSL2's `drvfs` mount exposes
NTFS metafile metadata even though content reads are denied:

```bash
stat -c '%s' '/mnt/c/$MFT'   # 2882011136   -> instant, no elevation
du -b        '/mnt/c/$MFT'   # 2882011136   -> also works
ls -la       '/mnt/c/$MFT'   # prints the size; "Permission denied" is a stderr warning and
                             # ls still exits 0. This is NOT a denial demo.

# the actual denial:
dd if='/mnt/c/$MFT' of=/dev/null bs=1024 count=1   # failed to open: Permission denied (exit 1)
```

> **Corrected 2026-08-30.** An earlier version used `ls -la` as the proof of denial. It is not:
> it succeeds and prints the size. Metadata is readable by several tools; only *content* reads
> are denied.

Measured on this machine:

| Volume | `$MFT` bytes | 1 KB slots |
|---|---|---|
| C: (269.5 GB) | 2,882,011,136 (2.68 GiB) | 2,814,464 |
| Y: (50 GB) | 2,883,584 | 2,816 |
| W: (320 GB) | 65,536 | 64 (NTFS minimum) |

Corroborated three ways: Y: enumerates completely at ~95% slot utilisation; W: sits at NTFS's
minimum allocation; and on C:, walking **19,961 System32 entries (files *and* directories)** via
`st_ino` hit a max record of **2,790,039** - 99.13% of the slot count, never above it. (Saying
"files" here sent an auditor to a files-only walk and a different number; entries is the word.)

**`st_ino` carries a constant +2 offset.** The low 48 bits are the record number **plus 2**,
proven against `fsutil file queryfileid` and five metafiles (`$MFT` 0->2, `$MFTMirr` 1->3,
`$AttrDef` 4->6, `$UpCase` 10->12, `$Extend` 11->13). Use `st_ino - 2`. `fsutil file queryfileid`
also works unprivileged.

**That makes the record size a measurement, not an assumption.** Max record 2,790,039 implies
`AllocatedRecordSize < 2,882,011,136 / 2,790,039 = 1,033.0 B`; against NTFS's documented 1,024 B
floor, C: is provably 1,024 - with ~9 bytes of slack.

**The case that breaks it:** volumes formatted with `format /L` / `Format-Volume -UseLargeFRS` use
**4,096-byte** records, making `slots = size/1024` **4x too high**. The `st_ino` bound only catches
that if some observed record exceeds `size/4096`; on a lightly-used large-FRS volume it will not
fire and the technique silently overcounts.

## Why it is worth documenting

Microsoft's KB 174619 documents an unprivileged method - *"type the `dir /a $mft` command on an
NTFS volume"* - but it **no longer works** on this build (`cmd.exe /c 'dir /a $mft'` ->
`File Not Found`). That is the strongest argument for documenting the WSL route, and the earlier
version of this item failed to make it.

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
3. **Quoting the 12.5% "MFT zone" as an estimator.** It is reserved free space for
   anti-fragmentation, not the `$MFT`'s size (KB 174619). The "ratios are undocumented" caveat is
   scoped to the `NtfsMftZoneReservation` 1-4 **settings**; the 12.5% default itself *is*
   documented.
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
10. **Assuming `slots = size/1024` without checking for large-FRS.** That is the one case where
    this table is silently 4x wrong. (An earlier version claimed `du`/`df` cannot see `$MFT`.
    False - `du -b` returns the exact size. What is true: `$MFT` is not enumerated by
    `ls /mnt/c`, so a recursive `du` never reaches it.)
