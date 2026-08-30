# Citation repair tooling

Regenerates the two Windows-native research reports from their original report objects,
resolving each `turnNsearchM` marker to a numbered or supporting bibliography entry while
preserving the citation IDs verbatim.

Committed 2026-08-30 because backlog item 10 prescribed these files while they existed only in
a scratch directory — making that item unfollowable.

## Layout

| File | Contents |
|---|---|
| `repair.py` | The generator. Takes a source markdown plus a primary and supporting TSV |
| `arrow-primary.tsv` | 34 rows: `num`, `turnID`, `title`, `url` |
| `arrow-support.tsv` | 18 rows: `turnID`, `title`, `url` |
| `duck-primary.tsv` | 39 rows: `num`, `turnID`, `title`, `url` |
| `duck-support.tsv` | 19 rows: `turnID`, `title` — **URLs missing, this is item 10** |

Supporting TSVs take an optional third column. `repair.py` emits a markdown link when a URL is
present and plain text when it is not.

## Regenerating

The source files are the **pre-repair** markdown, recoverable from git:

```bash
git show 5fa00b9:docs/apache-arrow-windows-native-research.md > /tmp/orig-arrow.md
git show 5fa00b9:docs/duckdb-windows-native-research.md       > /tmp/orig-duckdb.md

python3 repair.py /tmp orig-arrow.md  arrow-primary.tsv arrow-support.tsv  repaired-arrow.md
python3 repair.py /tmp orig-duckdb.md duck-primary.tsv  duck-support.tsv   repaired-duckdb.md
```

The TSVs must live in the same directory passed as the first argument, or adjust paths.

Expected output on success:

```
repaired-arrow.md:  markers=115 ids_used=52 unmapped=[] pua_left=0 mermaid=2 inline_links=225
repaired-duckdb.md: markers=160 ids_used=58 unmapped=[] pua_left=0 mermaid=1 inline_links=297
```

Any nonzero `pua_left`, any non-empty `unmapped`, or a changed mermaid count means stop.

## Both copies must stay identical

The reports live in two repos and are currently byte-identical (`md5sum`):

- `mftecmd/docs/{apache-arrow,duckdb}-windows-native-research.md`
- `ntfsight/docs/research/{apache-arrow,duckdb}-windows-native.md`

Regenerate once, copy to both, verify with `md5sum` before committing.

## Do not

- Resolve one report's IDs with the other's bibliography — the namespaces collide
  (`turn15search1` means different sources in each).
- Deduplicate DuckDB `[34]` and `[38]`. They are distinct citations into the same SQL Server 2025
  page. `turn7search6` (the 2022 page) is never cited in the report.
- Look URLs up on the live web instead of recovering them from the report object. That is how
  `beta.26120.1` was substituted for the actually-cited `beta.25323.1`.
