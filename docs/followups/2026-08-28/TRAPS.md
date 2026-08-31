# Cross-cutting traps

Failure modes that apply across items. All were hit for real.

## Environment

1. **`chmod +x` after every build.** MSBuild writes the `.exe` over the `\\wsl$` share without
   the ext4 exec bit. First run dies `Permission denied`. Recurs on **every** rebuild.
2. **Launch the exe by its Linux path, `winpath` only the arguments.** Bash cannot exec a UNC
   path — `\\wsl$\...\MFTECmd.exe` gives `command not found`.
3. **No `dotnet` in WSL.** Build with `/mnt/c/Program Files/dotnet/dotnet.exe`. Use
   `-f net9.0` alone while iterating; all three TFMs is far slower.
4. **`pwsh.exe`, never `powershell.exe`,** from WSL — 5.1 inherits a PS7 `PSModulePath` and
   fails to load its own modules.
5. **WSL interop hiccups.** `UtilAcceptVsock:273: accept4 failed 110` is transient. Retry
   **2-4 times** under load before diagnosing anything.
6. **`/mnt/c` is ~100x slower than native enumeration.** Time-box any `find /mnt/c` and prefer
   `robocopy /L` Windows-side.
7. **Private-use characters get stripped in transit.** The citation markers use U+E200/01/02.
   Heredocs dropped them nondeterministically. Build them with `chr(0xE200)` in a script file,
   never paste them literally.

## Evidence discipline

8. **Structure is not correctness.** The corrupt dictionary build had a valid schema, the right
   row count, and the right file size. Read values back and diff against a known-good baseline.
9. **Single-batch fixtures hide batch bugs.** `DefaultBatchSize` is 10,000; only `tdungan`
   (52,210 rows / 6 batches) exercises multi-batch behaviour.
10. **Fixtures under-represent real volumes.** ~126 B/row vs ~172-180 B/row, because
    `ParentPath` is 44% of bytes and depends on path shape.
11. **`grep -c` counts lines, not occurrences,** and misses tokens containing invisible
    characters. An early count of "122 junk tokens" was really 297.
12. **`find -maxdepth 4` misses the fixtures.** They are **8 components** below `~/dev` (9 for
    `NIST/DFR-16`); `-maxdepth 5/6/7` also fail. That is why an early search concluded no `$MFT`
    existed. Note `-maxdepth 9` finds **six**, not three - there is a duplicate fixture set in the
    standalone `~/dev/projects/github/mft` clone.

## Repo hygiene

13. **A peer session works in ntfsight.** Fetch before branching; it has renamed a branch
    mid-flight before.
14. **`docs/consolidate-documentation` is retired.** Base ntfsight work on `master`.
15. **Three trees hold the same paths.** `mftecmd/mft` (submodule, the build input),
    `~/dev/projects/github/mft` (standalone clone), and both fixture sets. Confirm which one you
    are reading before trusting a line number.
16. **The same two research reports exist in both repos** (mftecmd `docs/`, ntfsight
    `docs/research/`). Change both or neither.
17. **Untracked irreplaceable data lives in ntfsight**, and it contains **client-identifying
    information**. Never `git clean -fdx` there, and never commit it raw (item 09).

## Claims

17. **Never quote a number absent from `FACTS.md`.**
18. **Say which record count you mean** — slots, in-use, or rows. All three differ.
19. **Label extrapolation as extrapolation.** The 450GB→450MB error propagated into three
    documents and underpinned a "1000x+ reduction" headline because nobody checked the unit.
