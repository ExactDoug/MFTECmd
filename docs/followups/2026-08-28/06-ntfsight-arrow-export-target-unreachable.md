# 06 — The "15-30 MB Arrow IPC" export target is unreachable as built

**Repo:** ntfsight · **Severity:** wrong target · **Effort:** S · **Status:** open

## What is wrong

ntfsight docs target **15-30 MB** for the Arrow IPC export. The measured Arrow output for the
same volume was **406 MB** (2,366,478 records) — 13-27x over.

> **Sourcing caveat (added 2026-08-30).** The 406 MB figure is **not in the ntfsight repo**;
> `git log --all -S"406 MB"` returns nothing. It exists only in claude-mem obs #1053. Treat it as
> memory-sourced, not repo-verifiable.

With the Zstd compression now shipped in MFTECmd, that volume would land around **90-110 MB** —
but this is a **projection, not a measurement**. It applies the tdungan fixture's 4.58x ratio
(406 / 4.58 = 88.6), which is exactly the move ranked mistake #3 below warns against. It probably
runs conservative: the real volume's excess bytes are deep repetitive `ParentPath` values, which
compress better than the fixture's.
Better, still not 15-30 MB. Dictionary encoding would be the remaining lever and it is
**blocked upstream** (see mftecmd item 02).

## Where the figure appears

Verified at ntfsight `master` = `03ebea5`:

```
docs/reference/mft-in-memory-handling.md:17,47,437,747,876,908
docs/planning/development-roadmap.md:21,96,269,502
```

**10 sites, not 9** — an earlier version omitted `development-roadmap.md:502`
(`| Export size | 1.7 GB CSV | 15-30 MB Arrow IPC | Not implemented |`), precisely the miss that
ranked mistake #1 warns about.

**Do not touch `docs/research/portable-deployment.md:285`** — its "15-30 MB" is the
*self-contained trimmed binary* size, an unrelated coincidence.

## Proposed fix

Re-baseline against measurement: state the achieved figure (Zstd, ~90-110 MB projected for
2.4M records), keep 15-30 MB only if explicitly labelled as an aspiration contingent on
dictionary encoding, and cross-reference why that is currently blocked.

## The 10 most likely mistakes, ranked

1. **Blind find-and-replace of "15-30 MB".** It hits `portable-deployment.md:285`, which is
   about binary size and is correct as written.
2. **Quoting 406 MB as the post-Zstd figure.** 406 MB is *uncompressed*; Zstd shipped after
   that measurement was taken.
3. **Applying the tdungan compression ratio directly** — including in this item's own 90-110 MB
   projection, which is now labelled as such above. Real-volume rows cost ~172-180 B vs
   the fixture's ~126 B, because `ParentPath` is 44% of bytes and path-shape dependent.
4. **Recomputing the "~57-113x reduction" without noticing it is already corrected.** That
   line was fixed in the 450GB typo pass; do not regress it.
5. **Assuming the 2.37M-record volume still exists.** It does not — the machine was
   re-partitioned. Today's C: has ~2.81M slots.
6. **Treating the roadmap's target as measured.** It is marked "Untested" / "Not implemented".
7. **Editing `docs/` on the wrong branch.** The consolidated tree exists on `master` now;
   `docs/consolidate-documentation` is retired.
8. **Forgetting a peer session also works in this repo.** Fetch and check for concurrent
   branches before committing.
9. **Reporting compressed size without the codec.** `--arrowc` has three modes with a 4.6x
   spread between them.
10. **Deleting the aspiration entirely.** It is a legitimate goal; it just needs labelling as
    contingent, not achieved.
