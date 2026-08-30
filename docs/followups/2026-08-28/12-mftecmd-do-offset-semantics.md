# 12 — `--do` treats a documented byte offset as an entry index

**Repo:** mftecmd · **Severity:** latent bug · **Effort:** S · **Status:** open · **Found:** 2026-08-30 audit

## What is wrong

`MFTECmd/Program.cs:130-131` documents `--do` as a **byte offset**:

> `Offset of the $MFT FILE record to dump as decimal or hex. Ex: 5120 or 0x1400 Use --de or --debug to see offsets`

But `Program.cs:2112` **multiplies** the parsed value by `AllocatedRecordSize`, treating it as an
entry *index*:

```csharp
b.BaseStream.Seek(offset * _mft.FileRecords.Values.First().AllocatedRecordSize, 0);
```

A user following the help literally with `--do 0x1400` seeks to `0x1400 x 1024`.

Compounding it: the offsets `--debug` prints are genuine byte offsets (`FileRecord.Offset` is set
from `Mft.cs`'s byte index, `FileRecord.cs:43`), so the help's own suggested workflow produces
values that are wrong when fed back in.

Long-standing upstream (present since `aa4baa4`), not introduced by this fork.

## Relationship to item 01

Same call site, same two lines. Item 01 is the hardcoded `ReadBytes(1024)`; this is the semantic
mismatch in the seek above it. **Fixing item 01 alone leaves this untouched** — they should be
addressed together or explicitly sequenced.

## Proposed fix

Decide which semantic is intended, then make code and help agree. Changing the code is a
behaviour change for anyone scripting `--do`; changing the help is safer and probably correct,
since entry-index is what the code has always done.

## The 10 most likely mistakes, ranked

1. **Fixing item 01 and considering this call site done.** Two independent defects, adjacent lines.
2. **"Fixing" the code to match the help,** silently breaking every existing `--do` invocation.
3. **Assuming `--debug` offsets are entry numbers.** They are byte offsets, which is the trap.
4. **Testing with `--do 0`,** where index and offset coincide and nothing is revealed.
5. **Not passing `--dd`.** `--do` alone is rejected by validation at `Program.cs:446-478`.
6. **Reporting it upstream as this fork's bug.** It predates the fork.
7. **Changing help text without checking the `--de` help,** which is a different mechanism.
8. **Assuming this is unreachable.** It is reachable via `--dd` + `--do`.
9. **Fixing without a regression case** for a hex input, which is where the confusion bites.
10. **Rewriting the seek to use a literal 1024** and reintroducing item 01.
