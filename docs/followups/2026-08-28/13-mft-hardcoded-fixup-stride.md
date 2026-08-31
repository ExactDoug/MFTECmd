# 13 — Hardcoded 512-byte fixup stride in the MFT library

**Repo:** mft (submodule) · **Severity:** latent bug · **Effort:** S · **Status:** open · **Found:** 2026-08-30 audit

## What is wrong

`mft/MFT/FileRecord.cs:84` and `:102` hardcode `512` as the update-sequence-array (fixup) stride:

```csharp
var counter = 512;
...
counter += 512;
```

`mft/MFT/Other/FixupData.cs:27` documents it as *"the data expected at the end of each 512 byte
chunk"*. That stride is the **sector size**, not a constant.

Verified against the fixtures: all three have `FixupEntryCount = 3` = 1 + 1024/512, consistent
with 512-byte sectors and 1,024-byte records.

## Why it matters

On a 4Kn / large-sector volume, the fixup loop patches the wrong offsets — the **same trigger
condition as item 01**, and a worse failure, because fixups silently corrupt parsed record
content rather than throwing.

## Why item 01's remediation cannot find it

Item 01 says "grep the whole file for `1024`". This is a different literal (`512`), a different
file (`FileRecord.cs`), and a different repo (the `mft` submodule). Anyone scoping to item 01 as
written will miss it.

## Proposed fix

Derive the stride from `$Boot`'s bytes-per-sector rather than assuming 512. `mft/Boot/Boot.cs`
already parses the boot sector, so the value is available.

## The 10 most likely mistakes, ranked

1. **Fixing this in the standalone `~/dev/projects/github/mft` clone** and expecting mftecmd to
   pick it up. mftecmd consumes the **submodule**; a pointer bump is required (see PR #5's shape).
2. **Testing on the fixtures.** All three are 512-byte-sector / 1,024-byte-record; they cannot
   exercise this.
3. **Assuming `FixupEntryCount` is always 3.** It is `1 + record_size/sector_size`.
4. **Confusing sector size with cluster size.** The fixup stride is sectors; `AllocatedRecordSize`
   floors at cluster size. Different quantities.
5. **Fixing `FileRecord.cs` but not the other USA consumers** — check `$I30`/`$LogFile` paths for
   the same assumption.
6. **Breaking the common case.** 512 is right for nearly every volume; the fix must not regress it.
7. **Editing the submodule with a detached HEAD** and losing the commit.
8. **Opening the PR against the wrong repo.** This is `ExactDoug/MFT`, not `ExactDoug/MFTECmd`.
9. **Assuming fixups throw on mismatch.** They can silently produce wrong bytes, which is why
   this is worse than item 01.
10. **Treating this as a duplicate of item 01.** Same trigger, different defect, different repo.
