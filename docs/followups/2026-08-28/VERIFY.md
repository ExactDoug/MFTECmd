# Verification recipes

Copy-paste checks. Use these rather than inventing your own — several obvious-looking
alternatives give false passes.

## 0. Always first

```bash
docs/followups/2026-08-28/verify.sh        # run from the repo root
```
Any **FAIL** means stop.

## 1. Build (Windows-side, from WSL)

```bash
cd /mnt/c
WP=$(winpath ~/dev/projects/github/mftecmd)
"/mnt/c/Program Files/dotnet/dotnet.exe" build "$WP\\MFTECmd\\MFTECmd.csproj" -f net9.0 -c Debug -v m 2>&1 | tail -12
chmod +x ~/dev/projects/github/mftecmd/MFTECmd/bin/Debug/net9.0/MFTECmd.exe   # REQUIRED every time
```
Retry once on `accept4 failed 110` before investigating.

## 2. Run against a fixture

```bash
EXE=~/dev/projects/github/mftecmd/MFTECmd/bin/Debug/net9.0/MFTECmd.exe
OUT=/tmp/verify-out && mkdir -p $OUT
cd /mnt/c && "$EXE" \
  -f "$(winpath ~/dev/projects/github/mftecmd/mft/MFT.Test/TestFiles/tdungan/'$MFT')" \
  --flo --arrow "$(winpath $OUT)"
```
Use **tdungan** for anything batch-related. `xw`/`NIST` are single-batch.

**If you have just rebuilt, run the `chmod +x` from section 1 first.** This block is often run
alone, and a missing exec bit fails as `Permission denied`.

## 3. Validate Arrow content — the check that matters

Structure passing is not enough. This reads every value and compares against a baseline.

```python
import pyarrow as pa, pyarrow.ipc as ipc, glob
def load(d):
    f=sorted(glob.glob(f"{d}/*.arrow"))[-1]
    with pa.memory_map(f,'rb') as s: return ipc.open_file(s).read_all()
a,b = load("BASELINE_DIR"), load("NEW_DIR")
assert a.schema.equals(b.schema) and a.num_rows==b.num_rows
da,db = (t.to_pandas().sort_values(["EntryNumber","FileName","FileSize"]).reset_index(drop=True) for t in (a,b))
bad=[c for c in da.columns if not da[c].equals(db[c])]
print("IDENTICAL" if not bad else f"DIFFER: {bad}")
```
`.to_pandas()` is essential — it forces value materialisation. `read_all()` alone can succeed
on a corrupt dictionary-encoded file.

## 4. Dictionary bounds check (item 02 only)

Runnable as written. Column 2 is `ParentPath`, and it is a **plain `StringArray` today** -
dictionary encoding is not shipped (item 02), so this reports "plain StringArray". That is the
expected result, not evidence of a bug.

```python
import pyarrow as pa, pyarrow.ipc as ipc, pyarrow.compute as pc, glob
f = sorted(glob.glob("YOUR_DIR/*.arrow"))[-1]
with pa.memory_map(f, 'rb') as s:
    r = ipc.open_file(s)
    for i in range(r.num_record_batches):
        col = r.get_batch(i).column(2)                  # ParentPath
        if not hasattr(col, 'dictionary'):
            print(f"batch {i}: plain StringArray (expected today)"); continue
        assert pc.max(col.indices).as_py() < len(col.dictionary), f"batch {i} OUT OF BOUNDS"
```

## 5. Citation integrity (items 10 and any doc repair)

```python
import io,re
s=io.open(PATH,encoding='utf-8').read()
assert sum(s.count(c) for c in (chr(0xE200),chr(0xE201),chr(0xE202)))==0   # no raw markers
body=s.split("## Sources")[0]
assert not re.findall(r'(?<!`)turn\d+(?:search|view|news)\d+(?!`)', body)  # no stray IDs in prose
print("anchors:", len(re.findall(r'<a id="ref-', s)))   # Arrow 52, DuckDB 58
```

## 6. Size expectations

| Fixture | uncompressed | zstd |
|---|---|---|
| tdungan (52,210 rows) | 6,570,778 B | 1,434,514 B |
| xw (638 rows) | 71,538 B | — |

A `--arrowc none` run must reproduce the uncompressed bytes **exactly**.
