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

The MFT library already handles this correctly — `mft/MFT/Mft.cs` reads `AllocatedRecordSize`
from offset `0x1c` and iterates by that value. Only this one call site assumes 1024.

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
6. **Testing only through the CLI.** This path is `--dd`/`--do`/`--de` dump territory, not
   the `--csv`/`--arrow` path; a normal run never reaches it.
7. **Assuming 4096 is the only alternative.** NT4 permitted up to 64 KB. Do not swap one
   magic number for another.
8. **Building on Windows and forgetting `chmod +x`,** then misreading `Permission denied`
   as a code fault.
9. **Introducing a per-record property read inside a hot loop.** Hoist it; this sits near
   per-record work and `--flo` exists precisely to avoid per-record overhead.
10. **Bundling this with unrelated `Program.cs` changes,** making the one-line safety fix
    hard to review or revert.
