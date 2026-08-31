import io,re,sys,csv
SP,src,prim_f,sup_f,out=sys.argv[1:6]
S200,S201,S202=chr(0xE200),chr(0xE201),chr(0xE202)
MARK=re.compile(re.escape(S200)+"cite((?:"+re.escape(S202)+r"turn\d+(?:search|view|news)\d+)+)"+re.escape(S201))
prim={}   # id -> (num,title,url)
for r in csv.reader(io.open(f"{SP}/{prim_f}",encoding='utf-8'),delimiter='\t'):
    prim[r[1]]=(int(r[0]),r[2],r[3])
sup={}    # id -> (Sn,title,url or '')
for i,r in enumerate(csv.reader(io.open(f"{SP}/{sup_f}",encoding='utf-8'),delimiter='\t'),start=1):
    sup[r[0]]=(i,r[1],r[2] if len(r)>2 else '')
s=io.open(f"{SP}/{src}",encoding='utf-8').read()
seen=set();n_mark=0
def lab(t):
    if t in prim: return f"[[{prim[t][0]}]](#ref-{prim[t][0]})"
    return f"[[S{sup[t][0]}]](#ref-s{sup[t][0]})"
def repl(m):
    global n_mark; n_mark+=1
    ids=[x for x in m.group(1).split(S202) if x]
    seen.update(ids)
    return "".join(lab(t) for t in ids)
s=MARK.sub(repl,s)
out_lines=["","## Sources","",
"Numbering matches the original report's Sources panel. Citation IDs are preserved verbatim, as are the source URLs recorded at research time - including where a newer URL now exists.",""]
for tid,(n,t,u) in sorted(prim.items(),key=lambda kv:kv[1][0]):
    out_lines += [f'<a id="ref-{n}"></a>**[{n}]** `{tid}` - [{t}]({u})',""]
out_lines += ["### Supporting references","",
"Additional references attached to grouped inline citations in the underlying report object. These did not appear as separate numbered cards in the Sources panel.",""]
for tid,(n,t,u) in sorted(sup.items(),key=lambda kv:kv[1][0]):
    entry=f'<a id="ref-s{n}"></a>**[S{n}]** `{tid}` - ' + (f"[{t}]({u})" if u else t)
    out_lines += [entry,""]
s=s.rstrip("\n")+"\n"+"\n".join(out_lines)
io.open(f"{SP}/{out}","w",encoding='utf-8',newline='').write(s)
allids=set(prim)|set(sup)
print(f"{out}: markers={n_mark} ids_used={len(seen)} unmapped={sorted(seen-allids)} "
      f"pua_left={sum(s.count(c) for c in (S200,S201,S202))} mermaid={s.count('```mermaid')} "
      f"inline_links={len(re.findall(r'\[\[S?\d+\]\]\(#ref-',s))}")
