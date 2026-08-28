# 10 — 19 DuckDB supporting references have no URL

**Repo:** mftecmd + ntfsight (same file, both repos) · **Severity:** incomplete data · **Effort:** S · **Status:** open

## What is wrong

In the repaired DuckDB report, the 39 numbered sources all carry titles and URLs. The 19
**supporting** references carry citation ID and title only — they render as plain text, not
links:

```
<a id="ref-s1"></a>**[S1]** `turn1search0` - C++ API – DuckDB
```

Their URLs were not recoverable: supporting IDs appear only inside grouped citations, and the
`.docx` export collapsed every grouped citation to a single link, so no positional evidence
exists for them.

The Arrow report does **not** have this problem — its 18 supporting refs all have URLs,
because the bibliography supplied them directly.

## Proposed fix

Recover the 19 URLs from the raw Deep Research object (the same source that resolved the
39 primary IDs) and re-run the repair. The pipeline already supports it: `duck-support.tsv`
takes an optional third column, and `repair.py` emits a link when it is present.

## The 10 most likely mistakes, ranked

1. **Looking the URLs up on the live web instead of recovering them from the report object.**
   That reintroduces exactly the error that put `beta.26120.1` in place of the cited
   `beta.25323.1`. Cite what the report cited.
2. **Applying the Arrow bibliography to DuckDB IDs.** The namespaces collide —
   `turn15search1` means different sources in each report.
3. **Deduplicating IDs that share a URL.** `[34]` and `[38]` deliberately point at the same
   SQL Server 2025 page. Do not merge them.
4. **"Fixing" `[34]` to the SQL Server 2022 page.** It is 2025. The 2022 page
   (`turn7search6`) is never cited in the final report text.
5. **Re-running the `.docx` conversion** to try to recover them. The export does not contain
   them — that is the whole reason they are missing.
6. **Regenerating only one repo's copy.** The same file lives in mftecmd `docs/` and ntfsight
   `docs/research/` and must stay identical.
7. **Renumbering supporting refs.** `[S1]`-`[S19]` are referenced inline; changing the order
   breaks every anchor.
8. **Dropping the ID from the entry** while adding the URL. The ID is the point of the repair.
9. **Adding `?utm_source=chatgpt.com` tracking parameters** from a copied link.
10. **Editing the markdown by hand** instead of updating the TSV and re-running `repair.py`,
    so the two copies drift.
