# 01 — Hardcoded 1024-byte MFT record read

**Repo:** mftecmd · **Severity:** latent bug · **Effort:** S · **Status:** open

## What is wrong

`MFTECmd/Program.cs:2112-2114` seeks using the volume's *actual* record size, then reads a
hardcoded 1024 bytes — contradicting the comment on the line above it.

```csharp
b.BaseStream.Seek(offset * _mft.FileRecords.Values.First().AllocatedRecordSize, 0); // ... typically 1024, but don't assume that
...
var fileBytes = b.ReadBytes(1024);
```

The seek is right. The read is wrong on any volume whose `AllocatedRecordSize` is not 1024.

## Why it is real

NTFS derives record size from `$Boot`'s `ClustersPerFileRecordSegment`, a **signed byte**:
negative `n` means `2^|n|` bytes. The near-universal value is `-10` → 1024. But the record
size floors at the cluster size, so 4Kn / large-cluster volumes can produce 4096-byte records.

The MFT library already handles this correctly — `AllocatedRecordSize` is read at
`mft/MFT/FileRecord.cs:115` (offset `0x1c`), and `mft/MFT/Mft.cs:50-57` reads the same bytes into
a local it iterates by. Only this one call site in MFTECmd assumes 1024.

## Two related defects found during audit

**(a) A larger bug at the same call site.** `--do`'s help (`Program.cs:130-131`) documents a
*byte offset* ("Ex: 5120 or 0x1400"), but line 2112 **multiplies** the value by
`AllocatedRecordSize`, treating it as an entry index. Following the help literally seeks to
`0x1400 x 1024`. Long-standing upstream; fixing only the `ReadBytes(1024)` leaves it.

**(b) A sibling hardcode this item's remediation cannot find.** `mft/MFT/FileRecord.cs:84,102`
hardcode `512` for the update-sequence-array (fixup) stride — the *sector* size. On the same 4Kn
volumes this item is about, that loop patches wrong offsets. Different literal, different file,
different repo: "grep this file for 1024" will never surface it.

## Proposed fix

Read `AllocatedRecordSize` into a local and use it for both the seek and the read.

## How to verify

Hard part: **we have no non-1024 fixture.** All three test `$MFT` files use 1024-byte records.
Options, in order of preference:
1. Synthesise a 4096-byte-record `$MFT` fixture, or
2. Unit-test the offset arithmetic directly, or
3. At minimum, assert `AllocatedRecordSize == fileBytes.Length` and log a warning otherwise.

Do **not** claim this is fixed on the basis of the existing fixtures passing — they cannot
exercise the bug.

## The 10 most likely mistakes, ranked

1. **Declaring victory because the existing tests pass.** They cannot detect this; every
   fixture is 1024. A green run proves nothing here.
2. **Changing the read but not auditing for other 1024 assumptions.** Grep the whole file
   for `1024` before assuming this is the only site.
3. **Assuming `AllocatedRecordSize` is always populated.** It comes from the first FILE
   record; on a truncated or damaged `$MFT` that collection may be empty. `.First()` throws.
4. **Using `ActualRecordSize` instead of `AllocatedRecordSize`.** They differ (`0x18` vs
   `0x1c`). The seek uses Allocated; the read must match.
5. **"Fixing" the comment instead of the code** — deleting "but don't assume that" to make
   the file self-consistent, which preserves the bug and destroys the warning.
6. **Testing only through the CLI.** Reached only by `--dd` **plus** `--do` (validation at
   `Program.cs:446-478` makes them mutually required). `--de` is a separate block at `:2138` and
   never reaches the 1024 read. A normal `--csv`/`--arrow` run never touches it.
7. **Assuming 4096 is the only alternative.** NTFS documents 1,024 min / 4,096 max; do not swap
   one magic number for another. (An earlier version cited "NT4 up to 64 KB" — unsupported, and
   `mft/Boot/Boot.cs:256-264`'s `Math.Pow(2, 256-size)` would overflow `int` first.)
8. **Building on Windows and forgetting `chmod +x`,** then misreading `Permission denied`
   as a code fault.
9. **Introducing a per-record property read inside a hot loop.** Hoist it; this sits near
   per-record work and `--flo` exists precisely to avoid per-record overhead.
10. **Bundling this with unrelated `Program.cs` changes,** making the one-line safety fix
    hard to review or revert.
