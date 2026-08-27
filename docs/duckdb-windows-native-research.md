# DuckDB as a Windows-Native Embedded Analytics Stack

## Executive summary

**Bottom line:** **yes—DuckDB is already a native Windows embeddable database library, not merely a Python package that happens to run on Windows.** Windows is a fully supported DuckDB platform, including x86-64 and ARM64, and the project explicitly recommends the Visual Studio/MSVC compiler for Windows builds. For native applications, DuckDB exposes a **core-team-maintained, Primary-support C API** and distributes `libduckdb` binaries; the Windows documentation shows a native package containing a DLL, import library, and C/C++ headers. DuckDB's CMake build also defines both shared and static library targets. None of this requires Python at application runtime. [\[1\]](https://duckdb.org/docs/current/dev/building/overview?utm_source=chatgpt.com)

For **C#/.NET**, the answer is also substantially yes. **DuckDB.NET** provides an idiomatic ADO.NET provider plus low-level bindings, and its “Full” NuGet packages bundle the native DuckDB library. DuckDB's own current client-support matrix recognizes C#/.NET as an official **Secondary-tier client**, at version 1.5.5, maintained by Giorgi Dalakishvili; Secondary means it receives new features but is not covered by the same community-support guarantee as Primary clients. Thus DuckDB.NET has become considerably more first-class than an arbitrary third-party P/Invoke wrapper, although it is still not a core-team Primary client. [\[2\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com)

There is one important qualification to “no Python.” **Consuming a prebuilt DuckDB Windows DLL or the full DuckDB.NET NuGet package requires no Python.** However, the current canonical DuckDB Windows source-build recipe for Visual Studio invokes a Python helper, `scripts/windows_ci.py`, around CMake. The general build system itself is CMake/C++, but a *strictly Python-free, officially documented, release-equivalent source-build recipe for MSVC* is not presently documented. The optional amalgamation generator also runs through Python and is explicitly best-effort/unsupported. [\[3\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com)

I found **no first-class DuckDB WinRT component, COM server, UWP-specific library, Windows App SDK projection, MSIX/Store-specific SDK, or NuGet package targeting those APIs as a distinct integration layer** in the current DuckDB client matrix, Windows build documentation, DuckDB.NET documentation, or official Windows ODBC documentation. Consequently, WinRT/UWP/Store certification and packaging should be treated as **unspecified**, not as supported merely because ordinary Win32/.NET binaries work on Windows. [\[4\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com)

The Windows performance story is strong at the engine level but **not specially Windows-optimized in a way the primary documentation exposes**. DuckDB uses vectorized, multithreaded analytical execution, compressed columnar storage, adaptive memory management, and out-of-core spilling. Conversely, its optional/current jemalloc integration is **not available on Windows**, and the official build documentation says `-march=native` is not supported. I found no current primary-source Windows-vs-Linux benchmark, MSVC-vs-Clang benchmark, or documented Windows-specific SIMD dispatch policy; therefore the exact SIMD code-generation strategy of official MSVC builds should be recorded as **unspecified**. [\[5\]](https://duckdb.org/docs/current/internals/vector?utm_source=chatgpt.com)

For DuckDB's *exact* use case—**an in-process, embeddable, columnar analytical SQL database**—I did **not** find a Microsoft-authored or Windows-first alternative that is clearly superior overall. Apache DataFusion is a genuine high-performance embedded analytical query-engine peer but is Rust-first, lacks an equivalent persistent transactional database format, and has no comparable first-class .NET/ADO.NET integration. ClickHouse's chDB is another genuine in-process OLAP peer with C/C++ APIs, but current Windows guidance still points to WSL rather than a native Windows build. Microsoft.Data.Sqlite/SQLite has arguably the best pure Microsoft/.NET embedding story but is a row-oriented transactional engine rather than an OLAP column store. SQL Server Express LocalDB is extremely Windows-native and supports the SQL Server analytical ecosystem, but it starts a SQL Server Database Engine process rather than linking an analytical engine into the application, and Express has severe CPU/memory/parallel batch-mode limits relative to DuckDB's intended use. [\[6\]](https://datafusion.apache.org/?utm_source=chatgpt.com)

| Question | Finding |
|----|----|
| Native Windows DuckDB engine? | **Yes; full-support Windows platform, x86-64 and ARM64.** [\[7\]](https://duckdb.org/docs/current/dev/building/overview?utm_source=chatgpt.com) |
| Native DLL usable from MSVC/Win32? | **Yes.** Official Windows package/docs expose DLL, `.lib`, and headers. [\[8\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com) |
| Static library? | **Yes when building from source:** DuckDB's CMake install targets include `duckdb_static`; whether every Windows release provides a prebuilt static `.lib` separately is **unspecified**. [\[9\]](https://github.com/duckdb/duckdb/blob/main/src/CMakeLists.txt?utm_source=chatgpt.com) |
| Idiomatic .NET package? | **Yes:** DuckDB.NET ADO.NET plus low-level bindings; Full packages include native DuckDB. [\[10\]](https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com) |
| Official .NET status? | **Secondary DuckDB client**, not Primary; receives features but lacks Primary community-support guarantee. [\[11\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com) |
| Python needed at runtime? | **No.** Native/C++ and full .NET packaging embed/load the native engine directly. [\[12\]](https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com) |
| Python needed to build from source? | **Canonical Windows recipe currently uses a Python build helper; strictly Python-free supported recipe is unspecified.** [\[3\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com) |
| Visual Studio/MSVC supported? | **Yes; recommended Windows compiler.** [\[13\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com) |
| UWP/WinRT/COM/Store SDK? | **No dedicated supported component found; compatibility/certification unspecified.** [\[14\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com) |
| Best true DuckDB-equivalent on Windows? | **DuckDB itself.** The serious peers either lose native Windows/.NET integration or cease to be true in-process columnar OLAP databases. [\[15\]](https://datafusion.apache.org/?utm_source=chatgpt.com) |

## DuckDB’s Windows-native status

DuckDB describes itself as an **in-process analytical SQL database**, and Windows is not categorized as a compatibility or experimental target: the current build matrix lists Windows x86-64 and ARM64 among fully supported platforms, with stable and preview builds available. In contrast, MinGW Windows variants are only best-effort/compatibility platforms; MSVC is the recommended Windows toolchain. [\[16\]](https://duckdb.org/docs/current/dev/building/overview?utm_source=chatgpt.com)

### Native C and C++ integration

The most robust native Windows integration is the **DuckDB C API**. DuckDB's own client matrix gives C **Primary** status, and the C API is deliberately modeled somewhat after SQLite's C interface. Applications include `duckdb.h`, open either an in-memory or file-backed database, create connections, prepare statements, append vectors/data chunks, and consume results without a separate server. That is the interface I would use for a long-lived Win32 or MSVC product even when the host application itself is C++. [\[17\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com)

The project also has a C++ API, but the documentation has historically warned that it is an internal/native API whose compatibility is less stable than the C ABI. The current client matrix classifies C++ as **Secondary**, while C remains Primary. In practical Windows application architecture, therefore, a C++ application can link natively but should prefer the C API as its binary-stability boundary unless it explicitly accepts C++ API churn. [\[18\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com)

The official Windows build documentation gives a particularly concrete indication of native packaging: its Windows `libduckdb` example tells developers to obtain a package containing `.dll`**,** `.lib`**,** `.hpp`**, and** `.h` artifacts. DuckDB's CMake files define a shared `duckdb` target as well as `duckdb_static`, and both are installation targets. This means a conventional Visual C++ application can either deploy `duckdb.dll` beside the application or build/link DuckDB statically from source. The retrieved documentation does not establish that the `.lib` shipped in every binary ZIP is a self-contained static library—on Windows it normally accompanies the DLL as an import library—so the safe conclusion is: **prebuilt DLL + import library is explicitly documented; static linking is explicitly supported from the CMake source build.** [\[19\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com)

### C# and .NET

The .NET path is unusually good for a database that did not originate in the Microsoft ecosystem. **DuckDB.NET** supplies both low-level DuckDB bindings and an **ADO.NET provider**, with the latter documented as the recommended C# interface. Its package model separates managed-only packages from “Full” packages; `DuckDB.NET.Data.Full` combines the ADO.NET provider and native DuckDB library, while `DuckDB.NET.Bindings.Full` supplies the low-level binding plus the native engine. Therefore the common Visual Studio experience is essentially “add NuGet package, open `DuckDBConnection`, execute SQL.” No Python, JVM, Node runtime, Rust runtime, or local DuckDB server is needed. [\[20\]](https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com)

This is more than a random community wrapper: DuckDB's own 2026 client page lists **C# (.NET), maintainer Giorgi, Secondary, version 1.5.5**. DuckDB states that Secondary clients receive new features but do not have the community-support guarantee of Primary clients; all Primary and Secondary clients use the MIT license. DuckDB.NET's documentation additionally describes the implementation as both an ADO.NET provider and a low-level .NET wrapper around DuckDB's C API. [\[21\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com)

There is also a newer Microsoft-platform development option: **DuckDB.ExtensionKit**, announced in March 2026, allows C# developers to implement DuckDB scalar/table-function extensions and compile them using **.NET Native AOT** into native DuckDB extension binaries, including `win-x64`. The produced extension has no managed-runtime dependency when loaded. This is noteworthy evidence of deeper .NET integration, but it should not be confused with the normal embedding API: ExtensionKit is for *extending DuckDB*, remains **experimental**, and does not replace DuckDB.NET for embedding the database in a C# application. [\[22\]](https://duckdb.org/2026/03/20/duckdb-extensionkit-csharp?utm_source=chatgpt.com)

Microsoft itself now publishes a `Microsoft.DotNet.Interactive.DuckDB` package that integrates DuckDB with .NET Interactive and depends on DuckDB.NET packages. That is useful evidence of tooling ecosystem adoption, although it is notebook/tooling integration, not an alternative Microsoft-maintained DuckDB engine binding. [\[23\]](https://www.nuget.org/packages/Microsoft.DotNet.Interactive.DuckDB/1.0.0-beta.25323.1?utm_source=chatgpt.com)

### ODBC and other Windows plumbing

DuckDB also publishes a Windows-native ODBC driver. The official package contains `duckdb_odbc.dll`, a setup DLL, and an installer that registers the driver through the Windows ODBC infrastructure. This is appropriate for existing Windows applications whose native data-access abstraction is ODBC, including legacy C/C++ software, but it is a less direct embedding path than calling the C API because it adds the ODBC Driver Manager and registration/configuration layer. [\[24\]](https://duckdb.org/docs/current/clients/odbc/windows?utm_source=chatgpt.com)

The resulting Windows architecture is conventional rather than Python-centric:

```mermaid
flowchart LR
    A["C# / .NET application"] --> B["DuckDB.NET ADO.NET"]
    B --> C["DuckDB C ABI / native bindings"]

    D["C++ / Win32 / MSVC application"] --> C

    E["ODBC application"] --> F["duckdb_odbc.dll"]
    F --> G["DuckDB native engine"]
    C --> G

    G --> H["Native .duckdb columnar file"]
    G --> I["Parquet / CSV / JSON / extensions"]

    J["C# extension project"] --> K[".NET Native AOT"]
    K --> L["Native DuckDB extension"]
    L --> G
```

This reflects documented ADO.NET/native-binding, C API, ODBC, file-format, and Native-AOT extension paths. [\[25\]](https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com)

### WinRT, UWP, COM, and Store applications

Here the answer is materially weaker. The current official client list includes C, C++, C#, ODBC, and other cross-platform clients but no **WinRT**, **COM**, **UWP**, or **Windows App SDK** client. The Windows build page addresses conventional native Windows/MSVC builds rather than UWP/MSIX deployment, and the DuckDB.NET package documentation addresses ordinary .NET application packaging rather than a Windows Runtime component. [\[26\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com)

Accordingly, the defensible status is:

**Win32:** supported. **MSVC C/C++:** supported. **Modern .NET/C#:** supported through a Secondary client. **ODBC:** supported. **Native-AOT C# extensions:** experimental but supported as an extension-development mechanism. **COM:** unspecified/no dedicated component found. **WinRT:** unspecified/no dedicated component found. **UWP:** unspecified. **Microsoft Store/MSIX certification and deployment:** unspecified. [\[27\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com)

That distinction matters: “a DLL can theoretically be incorporated into some Windows packaging model” is not the same claim as DuckDB documenting and testing that packaging model.

## Build, embedding, and Windows performance

### Building with Visual Studio and MSVC

DuckDB's Windows page says Windows requires the **Microsoft Visual C++ Redistributable at both build time and runtime**, that Windows builds invoke CMake directly, and that the recommended compiler is Visual Studio/MSVC. MinGW/MSYS2 is available only for compatibility cases where the Visual Studio build is not feasible. [\[13\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com)

At the general build-system level, DuckDB requires CMake and a C++ compiler and officially supports Windows on x86-64 and ARM64. The source tree's CMake configuration creates both shared and static DuckDB targets and adds Windows/MSVC-specific system libraries and compiler settings. [\[28\]](https://duckdb.org/docs/current/dev/building/overview?utm_source=chatgpt.com)

There is, however, an awkward detail for the user's exact “all-Microsoft/no Python” requirement. The **current documented Visual Studio recipe follows DuckDB's CI workflow via** `python scripts/windows_ci.py cmake ...` **and then runs** `cmake --build`. The source is fundamentally C++/CMake, but DuckDB has chosen a Python helper as the canonical Windows configuration step. Its amalgamation generator likewise uses Python, and the amalgamation itself is best-effort rather than officially supported. [\[3\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com)

Therefore:

> **Runtime dependency test:** passes.  
> **Prebuilt-development test:** passes.  
> **Strictly Microsoft-only source-toolchain test:** not fully documented as passing.

A team that is prohibited from installing Python on its build agents can simply consume official native artifacts or NuGet-native packages. If policy instead demands recompiling DuckDB source internally with *only* Visual Studio/CMake and no Python anywhere in the build chain, the current official documentation does not provide a supported recipe with release-parity guarantees, so I would record that requirement as **unspecified pending validation against the exact DuckDB release/build configuration**. [\[3\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com)

One future-looking issue is also worth flagging: current DuckDB documentation says the 1.x build requires a C++11-capable compiler but that **DuckDB 2.0 will require C++17**. DuckDB published a v2.0 preview on August 17, 2026 and says the release is planned for fall 2026, so a product starting now should make C++17-capable MSVC its baseline rather than designing around the minimum needed by 1.5.x. [\[29\]](https://duckdb.org/docs/current/dev/building/overview?utm_source=chatgpt.com)

### Execution and analytical performance

DuckDB is architecturally designed for the requested workload. Its execution engine is **vectorized**: operators process fixed-size vectors rather than invoking operator logic row by row, with a standard vector size of 2,048 tuples. Its native storage format is column-oriented, divided into row groups and column segments, with compression integrated into storage and execution. [\[30\]](https://duckdb.org/docs/current/internals/vector?utm_source=chatgpt.com)

DuckDB parallelizes analytical work across CPU threads, with row groups acting as an important unit of parallelism. Its performance guide uses a default row-group size of 122,880 rows and cautions that simply configuring more threads—including all hardware threads on an SMT machine—is not always faster. That model maps naturally onto modern Windows workstations with many cores. [\[31\]](https://duckdb.org/docs/current/guides/performance/how_to_tune_workloads?utm_source=chatgpt.com)

Memory management is likewise OLAP-oriented rather than SQLite-like. DuckDB coordinates query intermediate memory with a buffer manager and can spill operations such as sorting, aggregation, joining, and window processing to temporary storage when the working set exceeds memory. DuckDB's own tuning examples even demonstrate cases in which compressed persistent or compressed in-memory data substantially outperform an uncompressed in-memory representation because reduced memory traffic outweighs decompression cost; those measurements are illustrative DuckDB benchmarks, not Windows-specific numbers. [\[32\]](https://duckdb.org/2024/07/09/memory-management?utm_source=chatgpt.com)

For external analytics, DuckDB directly reads formats including **Parquet, CSV, and JSON**, avoiding a mandatory ETL/load phase. This is one of the largest practical advantages over conventional embedded transactional databases on Windows. [\[33\]](https://duckdb.org/docs/lts/data/overview?utm_source=chatgpt.com)

### Windows-specific optimization caveats

The available evidence does **not** support claiming a special Windows-optimized fork or Windows-only performance layer. Rather, Windows runs the same high-performance vectorized engine. The primary documentation reviewed does not specify the instruction-set dispatch policy of the official MSVC binaries in enough detail to make a precise SSE/AVX/AVX2 claim. In fact, the build overview explicitly says the normal `-march=native` mechanism for compiling against the local machine's native instruction set is **not supported**. Thus “SIMD/vectorized execution exists” and “official Windows builds are aggressively CPU-dispatched for every available SIMD ISA” should not be conflated; the latter is **unspecified** in the reviewed material. [\[34\]](https://duckdb.org/docs/current/internals/vector?utm_source=chatgpt.com)

There is one concrete Windows disadvantage: as of DuckDB 1.5.3+, DuckDB can use **jemalloc** on supported systems, and Linux distributions ship with it by default, but DuckDB's current allocator documentation explicitly says **jemalloc is not available on Windows**. Workloads especially sensitive to allocator contention should therefore be benchmarked on the actual Windows/MSVC target rather than assuming Linux allocator characteristics. [\[35\]](https://duckdb.org/docs/current/internals/jemalloc?utm_source=chatgpt.com)

Likewise, I found **no official benchmark establishing a current Windows performance delta against Linux/macOS, or MSVC against other compilers**. DuckDB does publish extensive benchmark infrastructure and real-world analytical results—for example recent ClickBench/TPC-style testing—but its prominent 2026 published test used Apple hardware, and its earlier database benchmark results should not be generalized to Windows. [\[36\]](https://duckdb.org/docs/lts/dev/benchmark?utm_source=chatgpt.com)

### Concurrency and threading

DuckDB's concurrency design is specifically tuned for an embedded analytical process. In normal read-write mode, **one process owns the database**, while multiple threads inside that process can read and write concurrently. DuckDB uses MVCC plus optimistic concurrency control; appends do not conflict, and updates to different rows/tables can proceed concurrently, while conflicting writes to the same row cause one transaction to fail and be retried. [\[37\]](https://duckdb.org/docs/current/connect/concurrency?utm_source=chatgpt.com)

For C API usage, multiple connections are supported, and the recommended high-performance pattern is generally to use connections appropriate to the application's worker/thread structure rather than serializing every operation through one connection. [\[38\]](https://duckdb.org/docs/lts/clients/c/connect?utm_source=chatgpt.com)

Multiple processes can open the same database read-only. Multi-process **writing** is a different matter: DuckDB's Quack remote protocol was still documented as beta in the 1.5.x documentation, with maturity targeted for DuckDB 2.0 in fall 2026. That does not undermine the in-process use case the user asked about, but it means DuckDB 1.5.x should not be treated as a drop-in embedded equivalent to a multi-process SQL Server service for write-heavy shared databases. [\[39\]](https://duckdb.org/docs/current/connect/concurrency?utm_source=chatgpt.com)

### Storage, SQL, licensing, and maintenance

DuckDB has its own compressed, columnar, single-database storage format while also querying Parquet and other external data directly. Transactions are ACID and use snapshot-isolation/repeatable-read semantics. Its SQL dialect closely follows PostgreSQL conventions but has documented differences and DuckDB-specific analytical additions, so “PostgreSQL-like” is accurate whereas “full PostgreSQL compatibility” is not. [\[40\]](https://duckdb.org/docs/current/internals/storage?utm_source=chatgpt.com)

The stable line at the time of this research is **DuckDB 1.5.5**, while DuckDB 2.0 is in preview for fall 2026. The project is very actively maintained; DuckDB reported more than 10,000 commits since 1.5 in its v2.0 preview discussion and passed 40,000 GitHub stars in August 2026. Those popularity numbers are not a reliability benchmark, but the release and development cadence clearly show an actively maintained project rather than a dormant Windows port. [\[41\]](https://github.com/duckdb/duckdb/releases?utm_source=chatgpt.com)

DuckDB and its Primary/Secondary clients use the **MIT license**. A very current governance development occurred on **August 26, 2026**: DuckLabs announced that it intends to join AWS as a subsidiary in early September, while explicitly stating that DuckDB, DuckLake, Quack, and related projects will remain MIT-licensed and under the nonprofit DuckDB Foundation, with no change to project roadmap or governance model. This is important enough for a technology-selection report to record because it occurred one day before this research, but based on the project's announcement it does **not** presently change the embedding or licensing conclusions. [\[42\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com)

## Equivalent and adjacent alternatives

A useful distinction is necessary here. Some products are genuine **analytical-engine peers** but weak Windows/.NET citizens. Others are excellent **Windows embedded databases** but not analytical column stores. Treating both groups as equally “DuckDB equivalents” would produce a misleading comparison.

### Apache DataFusion

**What it is.** Apache DataFusion is an Apache Software Foundation project implementing a high-performance, extensible analytical query engine in Rust over Apache Arrow. Its own documentation describes a **columnar, streaming, multithreaded, vectorized execution engine** with SQL and DataFrame APIs, query optimization, and native support for Parquet, CSV, JSON, and Avro. Architecturally, it is one of the most bona fide DuckDB alternatives in this report. [\[43\]](https://datafusion.apache.org/?utm_source=chatgpt.com)

**Windows integration.** Windows is supported as a development environment, but the official Windows instructions require the Rust toolchain plus Visual C++ Build Tools; the documented workflow ultimately builds through `cargo`. This produces native machine code, so there is no Python runtime requirement, but it is plainly not an all-Microsoft development stack. I found no first-class ADO.NET provider, Microsoft-supported NuGet embedding package, COM layer, or WinRT projection analogous to DuckDB.NET. [\[44\]](https://datafusion.apache.org/contributor-guide/development_environment.html?utm_source=chatgpt.com)

**Embedding complexity.** Excellent for a Rust application; significantly higher for C#/C++. A .NET product would need to establish its own FFI/native wrapper or use a non-core community bridge. Moreover, DataFusion is principally a **query-engine library for building analytical systems**, not a packaged embedded transactional database with a DuckDB-like self-contained persistent database file. [\[43\]](https://datafusion.apache.org/?utm_source=chatgpt.com)

**Performance.** DataFusion is genuinely competitive. In a November 2024 Apache DataFusion post, its authors reported that a particular ClickBench snapshot against a partitioned 14-GB Parquet dataset on the same hardware was faster than DuckDB, chDB, and ClickHouse on the compared hot runs. That is credible evidence that the core engine can be in DuckDB's performance class; it is **not** evidence that DataFusion is still universally faster in August 2026, nor was it a Windows benchmark. [\[45\]](https://datafusion.apache.org/blog/2024/11/18/datafusion-fastest-single-node-parquet-clickbench/?utm_source=chatgpt.com)

**Concurrency and storage.** Query execution is parallel, streaming, and multithreaded. It works naturally on Arrow and external data sources, but a DuckDB-style ACID native database storage layer is not part of the core proposition, so database-writer concurrency semantics are largely not applicable at this layer. [\[43\]](https://datafusion.apache.org/?utm_source=chatgpt.com)

**SQL/tooling/license.** SQL is feature-rich and backed by a sophisticated optimizer, but because DataFusion is a toolkit for building systems rather than a complete relational DBMS, SQL/DDL/transactional parity with DuckDB is incomplete. The project uses the permissive **Apache License 2.0** and ASF governance. Windows development is oriented toward Rust tooling and Visual C++ prerequisites rather than Visual Studio/.NET database tooling. [\[46\]](https://datafusion.apache.org/_sources/user-guide/introduction.md.txt?utm_source=chatgpt.com)

**Recommendation:** choose it over DuckDB when the product itself is fundamentally a Rust/Arrow analytical engine and engine extensibility is more important than a packaged database. For conventional C#/C++ Windows applications, DuckDB is substantially easier to ship. [\[47\]](https://datafusion.apache.org/?utm_source=chatgpt.com)

### ClickHouse chDB

**What it is.** chDB is perhaps the closest conceptual peer: ClickHouse describes it as a fast **in-process OLAP engine powered by ClickHouse**. Current language integrations include C/C++, Rust, Go, Node/Bun, Python and ADBC, and the native C/C++ API exposes connections, regular queries, streaming results, and persistent-session behavior through `libchdb` and `chdb.h`. [\[48\]](https://clickhouse.com/docs/chdb/install?utm_source=chatgpt.com)

**Windows integration is the blocker.** Current ClickHouse Windows documentation still tells Windows users to run ClickHouse under **WSL2**, and the chDB repository's explicit Windows-support response says ClickHouse does not compile natively on Windows and that chDB should be used through WSL/WSL2. A later 2025 issue again asked for native Windows support. I found no 2026 official documentation announcing a native MSVC chDB library, Windows NuGet package, WinRT component, or native Windows build instructions. [\[49\]](https://clickhouse.com/docs/resources/support-center/knowledge-base/setup-installation/install-clickhouse-windows10?utm_source=chatgpt.com)

That disqualifies chDB under the user's strict definition even though its engine is an excellent functional peer. Requiring WSL means it is neither an ordinary Win32 in-process library nor a clean dependency for a Windows Store/.NET desktop application. [\[50\]](https://clickhouse.com/docs/resources/support-center/knowledge-base/setup-installation/install-clickhouse-windows10?utm_source=chatgpt.com)

**Performance/storage/SQL.** chDB inherits the ClickHouse analytical engine and supports a very broad range of formats; its C interface can operate on persistent sessions and ClickHouse table engines. ClickHouse SQL is a sophisticated analytical dialect, but it differs significantly from DuckDB's PostgreSQL-influenced SQL. Current chDB documentation also offers performance-oriented modes for large aggregations. [\[51\]](https://clickhouse.com/docs/chdb/install/c?utm_source=chatgpt.com)

**Concurrency/thread-safety.** The C documentation demonstrates persistent connections and streaming queries, but I did not find a clear current specification of the thread-safety or multi-writer concurrency contract comparable to DuckDB's dedicated concurrency page; therefore that attribute is **unspecified for purposes of this report**. [\[52\]](https://clickhouse.com/docs/chdb/install/c?utm_source=chatgpt.com)

**License.** chDB is open source under Apache 2.0. [\[53\]](https://clickhouse.com/docs/chdb?utm_source=chatgpt.com)

**Recommendation:** technically compelling if Windows-native integration stops being a requirement. Under the stated requirements, it is **not presently a viable DuckDB replacement**. [\[50\]](https://clickhouse.com/docs/resources/support-center/knowledge-base/setup-installation/install-clickhouse-windows10?utm_source=chatgpt.com)

### SQLite with Microsoft.Data.Sqlite

**What it is.** SQLite is one of the world's canonical embedded databases: a self-contained, serverless, zero-configuration **in-process** SQL library using a single cross-platform database file. It has exceptional maturity and long-term compatibility, but its architecture and optimizer are oriented around conventional tables/B-trees rather than DuckDB-style vectorized columnar OLAP. [\[54\]](https://sqlite.org/about.html?utm_source=chatgpt.com)

**Windows/.NET integration is outstanding.** Microsoft's `Microsoft.Data.Sqlite` sits on SQLitePCLRaw and can either bundle an SQLite native library or use Windows' `winsqlite3.dll`. The Microsoft documentation explicitly provides `SQLitePCLRaw.bundle_winsqlite3` for the Windows system SQLite library and supports supplying a custom native SQLite library. This is arguably more Microsoft-native at the API/package level than DuckDB.NET. [\[55\]](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/custom-versions?utm_source=chatgpt.com)

Native C/C++ embedding is also famously simple because SQLite itself is an in-process C library and distributes an amalgamation. The engine is therefore excellent for Win32, C++, desktop, application-file, cache, configuration, and transactional workloads. [\[56\]](https://sqlite.org/about.html?utm_source=chatgpt.com)

**Performance.** This is where it ceases to be a genuine DuckDB replacement. SQLite is not a columnar analytical execution engine; scans, grouping, large joins, and wide analytical aggregations do not benefit from the same columnar/vectorized architecture. I found no current primary-source Windows benchmark that would justify presenting SQLite as competitive with DuckDB for the requested OLAP workload. Its performance strengths are in a different workload class. [\[57\]](https://a1.sqlite.org/eqp.html?utm_source=chatgpt.com)

**Concurrency.** SQLite supports concurrent readers and, in WAL mode, readers can continue while a writer appends to the WAL, but it retains a fundamentally constrained writer model. Microsoft's .NET documentation further warns that `Microsoft.Data.Sqlite` objects themselves are not thread-safe and recommends separate connection objects where needed. [\[58\]](https://sqlite.org/wal.html?utm_source=chatgpt.com)

**Storage/SQL.** Storage is a durable single SQLite database file, not an OLAP column store. SQL is broad and mature but lacks DuckDB's analytics-focused type system, direct Parquet-centric workflow, and columnar execution characteristics. [\[59\]](https://sqlite.org/about.html?utm_source=chatgpt.com)

**License/maturity.** The SQLite engine itself is dedicated to the **public domain** and is exceptionally mature; the project states an intention to maintain SQLite through 2050. The licensing of ancillary Microsoft/SQLitePCLRaw wrapper components should be evaluated separately if material to a redistribution audit; the engine's public-domain status is unambiguous. [\[60\]](https://sqlite.org/copyright.html?utm_source=chatgpt.com)

**Recommendation:** superior to DuckDB when the product really needs a tiny, universally embedded **transactional row-store/application file**, not when the main job is large analytical scans and aggregations. [\[61\]](https://sqlite.org/about.html?utm_source=chatgpt.com)

### SQL Server Express LocalDB

**What it is.** LocalDB is Microsoft's developer-focused lightweight packaging of the SQL Server Express Database Engine. It has zero/low-administration startup semantics and is deeply integrated into Windows and Visual Studio. However, it is **not an in-process database library**: connections automatically start the required SQL Server infrastructure, and each LocalDB instance runs as a separate user-mode Database Engine process. [\[62\]](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17&utm_source=chatgpt.com)

That distinction alone prevents LocalDB from being a literal DuckDB equivalent. An application talks to a local SQL Server instance; it does not call a linked query engine in its own address space. [\[62\]](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17&utm_source=chatgpt.com)

**Integration and tooling.** This is the strongest Microsoft-native candidate operationally. LocalDB can be installed through the SQL Server media or the Visual Studio Installer; applications use normal SQL Server technologies such as Microsoft ADO.NET/SqlClient, native ODBC, T-SQL, SQL Server Management Studio/SQL tooling, and SSDT/Visual Studio. [\[63\]](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17&utm_source=chatgpt.com)

**Analytical capability.** SQL Server itself has a highly developed **columnstore** architecture. Microsoft describes columnstore indexes as its standard structure for large data-warehouse fact tables and reports up to 10× query-performance and compression improvements over traditional row-oriented storage for appropriate workloads. SQL Server 2025 supports both clustered and nonclustered columnstore indexes. These are vendor performance claims/engineering guidance, not comparisons with DuckDB. [\[64\]](https://learn.microsoft.com/en-us/sql/relational-databases/indexes/columnstore-indexes-overview?view=sql-server-ver17&utm_source=chatgpt.com)

There is an important qualification for LocalDB specifically. Microsoft says LocalDB has the same limitations as SQL Server Express. Microsoft also documents columnstore availability across SQL Server editions, but the passages reviewed do not explicitly give a LocalDB-specific columnstore support statement for every 2025 option. Therefore **Express-level columnstore capability is documented; exact LocalDB-specific availability of every columnstore variant should be treated as unspecified unless validated on the target LocalDB build.** [\[65\]](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17&utm_source=chatgpt.com)

Even assuming usable columnstore indexes, Express is heavily constrained compared with an unconstrained analytics workstation: SQL Server 2025 Express is limited to the lesser of **one socket or four cores**, a **1,410-MB buffer pool**, **352 MB of columnstore segment cache**, and a **50-GB maximum relational database**; batch-mode degree of parallelism for Express is limited to **1**. Several data-warehouse optimizations are absent from Express. Those caps make LocalDB a poor choice for using all CPU/RAM on a serious single-machine analytical workload. [\[66\]](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2025?view=sql-server-ver17&utm_source=chatgpt.com)

**Concurrency.** Since LocalDB is the SQL Server Database Engine in a separate process, its concurrency model is the conventional multi-session SQL Server model rather than DuckDB's single owning application process. Multiple LocalDB engine processes/instances can run, all using installed shared binaries. [\[62\]](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17&utm_source=chatgpt.com)

**Storage/SQL.** It uses normal SQL Server databases and T-SQL rather than a portable DuckDB file and DuckSQL/PostgreSQL-style dialect. T-SQL is a much broader enterprise database ecosystem in many respects, but it does not provide DuckDB's frictionless “open Parquet/CSV and query it inside this process” architecture. [\[63\]](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17&utm_source=chatgpt.com)

**License.** SQL Server Express 2025 is a **free** Microsoft edition suitable for development and production desktop/web/small-server applications, but it remains proprietary Microsoft software rather than MIT/Apache/public-domain software. Redistribution and deployment remain governed by Microsoft's SQL Server license terms. [\[67\]](https://www.microsoft.com/en-us/sql-server/sql-server-downloads?utm_source=chatgpt.com)

**Recommendation:** excellent when the real requirement is “Microsoft-native local SQL Server/T-SQL with Visual Studio tooling,” especially where production can later move to full SQL Server. It is not the best answer when the requirement is specifically “link an OLAP engine into this process and use all local workstation resources.” [\[68\]](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17&utm_source=chatgpt.com)

## Comparison matrix

The distinction between **exact peer**, **analytical-engine peer**, and **adjacent substitute** is central to interpreting this table. It is an analytical classification based on the documented architectures above. [\[69\]](https://duckdb.org/docs/current/clients/c/overview?utm_source=chatgpt.com)

| Attribute | **DuckDB** | **Apache DataFusion** | **ClickHouse chDB** | **SQLite / Microsoft.Data.Sqlite** | **SQL Server Express LocalDB** |
|----|----|----|----|----|----|
| DuckDB-use-case fidelity | **Exact fit**: embedded analytical DBMS | **High as query engine**, lower as packaged DB | **Very high engine fit** | Low: embedded DB, wrong storage/execution class | Medium: strong analytics DB features, wrong process model |
| In-process | **Yes** [\[11\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com) | **Yes, as library** [\[70\]](https://datafusion.apache.org/?utm_source=chatgpt.com) | **Yes on supported platforms** [\[52\]](https://clickhouse.com/docs/chdb/install/c?utm_source=chatgpt.com) | **Yes** [\[56\]](https://sqlite.org/about.html?utm_source=chatgpt.com) | **No; separate user-mode SQL Server process** [\[62\]](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17&utm_source=chatgpt.com) |
| Native Windows | **Yes, full support** [\[7\]](https://duckdb.org/docs/current/dev/building/overview?utm_source=chatgpt.com) | Yes through Rust/Windows toolchain [\[71\]](https://datafusion.apache.org/contributor-guide/development_environment.html?utm_source=chatgpt.com) | **No native Windows path found; WSL documented** [\[50\]](https://clickhouse.com/docs/resources/support-center/knowledge-base/setup-installation/install-clickhouse-windows10?utm_source=chatgpt.com) | **Yes**; can use `winsqlite3.dll` [\[55\]](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/custom-versions?utm_source=chatgpt.com) | **Yes, Windows-first** [\[62\]](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17&utm_source=chatgpt.com) |
| x64 | Yes [\[7\]](https://duckdb.org/docs/current/dev/building/overview?utm_source=chatgpt.com) | Yes | Linux/WSL path on Windows | Yes | Yes |
| ARM64 Windows | Full platform support documented; exact C-library binary packaging details beyond platform-build availability are not separately specified here. [\[7\]](https://duckdb.org/docs/current/dev/building/overview?utm_source=chatgpt.com) | Rust target-dependent; exact support matrix unspecified here | No native Windows support | Windows/system-package dependent | Exact LocalDB ARM64 status not established in reviewed sources; **unspecified** |
| Idiomatic C#/.NET | **Yes: ADO.NET + low-level bindings** [\[72\]](https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com) | No comparable core .NET API found | No .NET integration in current chDB language list [\[73\]](https://clickhouse.com/docs/chdb/install?utm_source=chatgpt.com) | **Excellent: Microsoft.Data.Sqlite** [\[55\]](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/custom-versions?utm_source=chatgpt.com) | **Excellent: standard SQL Server .NET ecosystem** [\[74\]](https://www.microsoft.com/en-us/sql-server/sql-server-downloads?utm_source=chatgpt.com) |
| NuGet native bundle | **Yes: Full packages bundle DuckDB native library** [\[72\]](https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com) | No first-class equivalent found | No Windows-native package found | Yes through Microsoft.Data.Sqlite/SQLitePCLRaw bundles [\[55\]](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/custom-versions?utm_source=chatgpt.com) | Client packages exist, but engine installed separately |
| Native C/C++ | **Excellent; Primary C API** [\[75\]](https://duckdb.org/docs/current/clients/c/overview?utm_source=chatgpt.com) | Rust-native; C++ requires bridge | Excellent API, **but not native Windows** [\[76\]](https://clickhouse.com/docs/chdb/install/c?utm_source=chatgpt.com) | Excellent C API | ODBC/native SQL client; engine out of process [\[74\]](https://www.microsoft.com/en-us/sql-server/sql-server-downloads?utm_source=chatgpt.com) |
| Static link | **Source CMake target available** [\[9\]](https://github.com/duckdb/duckdb/blob/main/src/CMakeLists.txt?utm_source=chatgpt.com) | Rust static/native integration possible; exact Windows embedding recipe application-specific | Native library conceptually, but Windows unavailable | Straightforward SQLite amalgamation/native link | No |
| Native DLL | **Yes** [\[13\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com) | Can produce native artifacts; no DuckDB-like Windows SDK package | No supported Windows DLL found | Yes / system `winsqlite3.dll` [\[55\]](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/custom-versions?utm_source=chatgpt.com) | Engine binaries, but not an embeddable query DLL |
| COM / WinRT | **Not found; unspecified** | Not found | Not found | No core requirement; Windows-specific projection unspecified | SQL Server APIs rather than COM/WinRT embedding |
| UWP / Store first-class | **Unspecified/not documented** | Unspecified | No native Windows | Potential platform-specific SQLite paths, but exact current Store support outside this research scope | Not an embedded Store-oriented architecture |
| Visual Studio / MSVC | **Recommended native compiler; excellent .NET/NuGet path** [\[77\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com) | Visual C++ Build Tools are prerequisite, but Rust/Cargo drives build [\[71\]](https://datafusion.apache.org/contributor-guide/development_environment.html?utm_source=chatgpt.com) | No native MSVC build | Excellent | **Excellent; VS installer integration** [\[62\]](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17&utm_source=chatgpt.com) |
| Non-Microsoft runtime needed after deployment | **No for native C/C++; ordinary .NET only for C#** [\[72\]](https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com) | No interpreter, but Rust-built native library/toolchain | WSL/Linux environment on Windows | No; native or .NET | No |
| Strictly Python-free source build | **Official current MSVC recipe does not establish this; canonical helper uses Python** [\[13\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com) | Python unnecessary, but Rust required | Linux toolchain | Yes | Not source-build model |
| Columnar execution/storage | **Yes / yes** [\[78\]](https://duckdb.org/docs/current/internals/vector?utm_source=chatgpt.com) | **Yes execution / external Arrow-columnar data** [\[70\]](https://datafusion.apache.org/?utm_source=chatgpt.com) | **Yes, ClickHouse OLAP engine** [\[52\]](https://clickhouse.com/docs/chdb/install/c?utm_source=chatgpt.com) | **No; row/B-tree oriented** [\[79\]](https://a1.sqlite.org/eqp.html?utm_source=chatgpt.com) | SQL Server columnstore indexes available at edition level; LocalDB-specific details should be verified [\[80\]](https://learn.microsoft.com/en-us/sql/relational-databases/indexes/columnstore-indexes-overview?view=sql-server-ver17&utm_source=chatgpt.com) |
| Native persistent DB format | **Yes, compressed DuckDB format** [\[81\]](https://duckdb.org/docs/current/internals/storage?utm_source=chatgpt.com) | Not as a DuckDB-like core database format | ClickHouse table/storage mechanisms | **Yes, single SQLite file** [\[56\]](https://sqlite.org/about.html?utm_source=chatgpt.com) | SQL Server database files |
| Parquet-first analytics | **Excellent; direct read/write** [\[33\]](https://duckdb.org/docs/lts/data/overview?utm_source=chatgpt.com) | **Excellent** [\[46\]](https://datafusion.apache.org/_sources/user-guide/introduction.md.txt?utm_source=chatgpt.com) | **Excellent** | Not native core use case | Not comparable in-process/file-query workflow |
| Vectorized execution | **Yes** [\[82\]](https://duckdb.org/docs/current/internals/vector?utm_source=chatgpt.com) | **Yes** [\[70\]](https://datafusion.apache.org/?utm_source=chatgpt.com) | ClickHouse analytical execution | Not DuckDB-style | Batch/columnstore execution available in SQL Server, but Express DOP restricted [\[83\]](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2025?view=sql-server-ver17&utm_source=chatgpt.com) |
| Query parallelism | **Strong, configurable** [\[31\]](https://duckdb.org/docs/current/guides/performance/how_to_tune_workloads?utm_source=chatgpt.com) | **Strong, multithreaded** [\[70\]](https://datafusion.apache.org/?utm_source=chatgpt.com) | Strong engine; exact chDB thread contract unspecified | Limited relative to OLAP engines | Express batch mode DOP = **1** [\[83\]](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2025?view=sql-server-ver17&utm_source=chatgpt.com) |
| Out-of-core | **Yes; spill-aware** [\[84\]](https://duckdb.org/2024/07/09/memory-management?utm_source=chatgpt.com) | Streaming; exact DB spill semantics workload-specific | ClickHouse engine capabilities; chDB specifics not fully evaluated | Uses conventional pager/temp facilities, not equivalent OLAP spill design | Yes at SQL Server engine level but heavily memory-limited in Express |
| Single-process concurrent writers | **Yes via MVCC/OCC** [\[37\]](https://duckdb.org/docs/current/connect/concurrency?utm_source=chatgpt.com) | DB transaction semantics not core feature | **Unspecified** | WAL supports reader/writer concurrency but writer model is restricted [\[85\]](https://sqlite.org/wal.html?utm_source=chatgpt.com) | Multi-session SQL Server engine |
| Multi-process concurrent writers | Quack still beta in 1.5.x; traditional native-file mode not designed for that [\[37\]](https://duckdb.org/docs/current/connect/concurrency?utm_source=chatgpt.com) | Not applicable as packaged DB | Unspecified | Conventional SQLite limitations | **Yes through server process** |
| SQL style | Rich DuckSQL, PostgreSQL-influenced [\[86\]](https://duckdb.org/2026/08/20/duckdb-20-peg-parser?utm_source=chatgpt.com) | Rich analytical SQL, not complete DBMS parity [\[46\]](https://datafusion.apache.org/_sources/user-guide/introduction.md.txt?utm_source=chatgpt.com) | ClickHouse SQL | SQLite SQL | T-SQL |
| Analytical SQL parity vs DuckDB | Baseline | High query-engine capability; lower transactional/DBMS surface | High but dialect differs | Medium/low for advanced OLAP use | High in different areas; dialect and execution model differ |
| Windows-specific SIMD documentation | **Unspecified**; vectorization documented, `-march=native` unsupported [\[87\]](https://duckdb.org/docs/current/dev/building/overview?utm_source=chatgpt.com) | No Windows-specific claim used here | Not applicable natively | Not relevant as OLAP peer | Express lacks some higher-edition SIMD scalability enhancements [\[83\]](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2025?view=sql-server-ver17&utm_source=chatgpt.com) |
| Windows allocator issue | **jemalloc unavailable on Windows** [\[35\]](https://duckdb.org/docs/current/internals/jemalloc?utm_source=chatgpt.com) | Rust allocator/config dependent | N/A natively | Conventional SQLite allocation | SQL Server-managed |
| Relevant published performance evidence | Strong DuckDB benchmark history; no official Windows comparison found [\[88\]](https://duckdb.org/docs/lts/dev/benchmark?utm_source=chatgpt.com) | 2024 ClickBench Parquet result beat compared engines on that snapshot/hardware [\[45\]](https://datafusion.apache.org/blog/2024/11/18/datafusion-fastest-single-node-parquet-clickbench/?utm_source=chatgpt.com) | Strong ClickHouse heritage; no native Windows benchmark | No relevant OLAP benchmark found | Microsoft cites up to 10× columnstore gains vs rowstore, not vs DuckDB [\[89\]](https://learn.microsoft.com/en-us/sql/relational-databases/indexes/columnstore-indexes-overview?view=sql-server-ver17&utm_source=chatgpt.com) |
| Resource caps | Application/OS resources; DuckDB configuration controls memory/threads | Application resources | Application resources where supported | Application resources | **Express: 4 cores, ~1.4-GB buffer pool, 352-MB columnstore cache, 50-GB DB** [\[83\]](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2025?view=sql-server-ver17&utm_source=chatgpt.com) |
| License | **MIT** [\[42\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com) | **Apache 2.0** [\[46\]](https://datafusion.apache.org/_sources/user-guide/introduction.md.txt?utm_source=chatgpt.com) | **Apache 2.0** [\[53\]](https://clickhouse.com/docs/chdb?utm_source=chatgpt.com) | **SQLite engine: public domain** [\[90\]](https://sqlite.org/copyright.html?utm_source=chatgpt.com) | **Proprietary Microsoft; Express free** [\[74\]](https://www.microsoft.com/en-us/sql-server/sql-server-downloads?utm_source=chatgpt.com) |
| Maturity/maintenance | **High and very active; 1.5.5 stable, 2.0 imminent** [\[91\]](https://github.com/duckdb/duckdb/releases?utm_source=chatgpt.com) | High, ASF project | Active, but Windows gap is decisive | **Extremely high/long-lived** [\[56\]](https://sqlite.org/about.html?utm_source=chatgpt.com) | **High, Microsoft-supported SQL Server family** [\[92\]](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2025?view=sql-server-ver17&utm_source=chatgpt.com) |
| Overall fit for requested requirement | **Best** | **Second-best engine architecture, weaker Windows stack** | **Excellent engine but fails native-Windows requirement** | **Excellent embedding, wrong analytics architecture** | **Excellent Windows ecosystem, wrong embedding/resource model** |

## Recommendations

**For a new C++/Win32/MSVC analytical application, choose DuckDB and treat its C API as the product ABI.** It is the only option examined that simultaneously gives you a fully supported native Windows target, direct in-process execution, compressed columnar storage, analytical SQL, direct Parquet-style analytics, multithreading, ACID persistence, and a mature stable C interface. Build/link against `libduckdb`, prefer the stable C ABI even from C++, and either deploy the official DLL/import-library combination or create `duckdb_static` in your controlled source build. [\[93\]](https://duckdb.org/docs/current/clients/c/overview?utm_source=chatgpt.com)

**For C#/.NET, use DuckDB.NET—specifically a Full package when self-contained native deployment is desired.** The ADO.NET interface is idiomatic enough that the integration should feel like an ordinary .NET database provider rather than a foreign-language bridge, and the Full package brings the native engine with it. The principal governance caveat is the DuckDB **Secondary** support tier rather than Primary. For a mission-critical product, that is a support-policy consideration, not a fundamental technical deficiency. [\[94\]](https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com)

A reasonable C# deployment stack is therefore:

    Application
       │
       ├─ DuckDB.NET.Data.Full
       │     ├─ ADO.NET managed provider
       │     ├─ low-level DuckDB bindings
       │     └─ native DuckDB engine
       │
       └─ .duckdb / Parquet / CSV / JSON data

The Full-package architecture and native-library bundling are explicitly documented by DuckDB.NET. [\[72\]](https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com)

**For C# code that must itself become native DuckDB extension code**, DuckDB.ExtensionKit deserves monitoring. Native AOT can produce `win-x64` native extensions with no managed runtime required at load time, but the toolkit remains experimental and should not yet be treated as a substitute for the mature C API or normal DuckDB.NET embedding. [\[22\]](https://duckdb.org/2026/03/20/duckdb-extensionkit-csharp?utm_source=chatgpt.com)

**Do not base a product requirement on UWP/WinRT/Windows Store first-class support without an explicit prototype and certification test.** I found no dedicated DuckDB Windows Runtime component or Store-specific support declaration. For ordinary unpackaged Win32/.NET desktop applications that gap is largely irrelevant; for a constrained app-container or Store deployment it becomes a real engineering unknown and should remain marked **unspecified** until proven against the exact target framework and packaging model. [\[95\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com)

**If a strictly Python-free *source* build is a contractual requirement, this is DuckDB's most notable fit gap.** The runtime is cleanly native, but the official current Visual Studio build recipe uses a Python helper. Prebuilt native binaries avoid that issue entirely; an organization that mandates reproducible from-source builds with only Microsoft tooling should validate a direct CMake/MSVC process and compare its output/configuration against the official build before calling it supported. [\[3\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com)

**Apache DataFusion is the most technically credible alternative when “embedded analytical engine” matters more than “Microsoft development stack.”** It has demonstrated DuckDB-class—or, in a historical Parquet ClickBench snapshot, better—query-engine performance, and its Arrow/Rust architecture is excellent for building a custom analytics platform. But choosing it for a C# Windows desktop product means creating integration and persistence infrastructure that DuckDB already supplies. [\[96\]](https://datafusion.apache.org/blog/2024/11/18/datafusion-fastest-single-node-parquet-clickbench/?utm_source=chatgpt.com)

**chDB would be a serious head-to-head candidate if native Windows support existed. It currently does not meet the requirement.** Its C/C++ API and ClickHouse engine are legitimately close to DuckDB's analytical proposition, but a requirement for WSL immediately breaks “embedded directly into a native Windows application.” Re-evaluate chDB only if ClickHouse publishes a supported MSVC/Windows-native build in the future. [\[97\]](https://clickhouse.com/docs/chdb/install/c?utm_source=chatgpt.com)

**Use SQLite/Microsoft.Data.Sqlite when the workload is actually embedded OLTP/application storage with occasional analytics.** It wins on tiny deployment footprint, extraordinary compatibility, public-domain engine licensing, Microsoft-native .NET packaging, and longevity. It should not be selected because it happens to be embedded if large scans, joins, aggregation, Parquet analytics, and columnar execution are the core workload. [\[98\]](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/custom-versions?utm_source=chatgpt.com)

**Use SQL Server LocalDB when Microsoft ecosystem fidelity and T-SQL compatibility matter more than in-process execution.** It is particularly attractive if the local-development database is supposed to evolve into SQL Server in production. It is not a compelling DuckDB replacement for local OLAP because its separate-process architecture and Express limits—four cores, small memory/cache allowances, DOP 1 for batch mode—work directly against the “use this Windows machine as a fast embedded analytical engine” objective. [\[68\]](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17&utm_source=chatgpt.com)

The overall ranking for the exact stated requirement is therefore:

| Rank | Choice | Assessment |
|----|----|----|
| **Best overall** | **DuckDB native C API /** `libduckdb` | Closest possible fit for native MSVC/Win32 embedding. |
| **Best .NET** | **DuckDB.NET.Data.Full** | Idiomatic ADO.NET, bundled native engine; Secondary rather than Primary client support. |
| **Best engine-building alternative** | **Apache DataFusion** | Superb analytical engine, but Rust-first and less turnkey as a database/.NET dependency. |
| **Potential future peer** | **chDB** | Highly credible OLAP peer, presently disqualified by lack of supported native Windows build. |
| **Best transactional embedded fallback** | **SQLite / Microsoft.Data.Sqlite** | Excellent Windows/.NET embedding, not DuckDB-class OLAP architecture. |
| **Best Microsoft database fallback** | **SQL Server Express LocalDB** | Deepest Microsoft tooling/T-SQL integration, but out-of-process and resource-constrained. |

That ranking is an inference from the documented integration, execution, storage, concurrency, and Windows-support characteristics above rather than a claim derived from a single benchmark. [\[99\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com)

## Primary-source links and research notes

The most important DuckDB primary references are the official [Windows build instructions](https://duckdb.org/docs/current/dev/building/windows), [build overview](https://duckdb.org/docs/current/dev/building/overview), [client support matrix](https://duckdb.org/docs/current/clients/overview), [C API overview](https://duckdb.org/docs/current/clients/c/overview), [ODBC on Windows](https://duckdb.org/docs/current/clients/odbc/windows), [vectorized execution documentation](https://duckdb.org/docs/current/internals/vector), [jemalloc/allocator documentation](https://duckdb.org/docs/current/internals/jemalloc), and [concurrency documentation](https://duckdb.org/docs/current/connect/concurrency). [\[100\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com)

For .NET specifically, see [DuckDB.NET Getting Started](https://duckdb.net/docs/getting-started.html), [DuckDB.NET introduction](https://duckdb.net/docs/introduction.html), and DuckDB's March 2026 [DuckDB.ExtensionKit / C# Native AOT announcement](https://duckdb.org/2026/03/20/duckdb-extensionkit-csharp). [\[101\]](https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com)

For project maturity and the unusually recent governance change, see DuckDB's [v2.0 preview](https://duckdb.org/2026/08/17/duckdb-20-highlights) and the **August 26, 2026** [DuckLabs-to-AWS announcement](https://duckdb.org/2026/08/26/ducklabs-to-join-aws), which explicitly states that the projects remain MIT-licensed under the nonprofit DuckDB Foundation. [\[102\]](https://duckdb.org/2026/08/17/duckdb-20-highlights?utm_source=chatgpt.com)

For alternatives, the principal primary references are Apache's [DataFusion documentation](https://datafusion.apache.org/) and its historical [ClickBench/Parquet performance report](https://datafusion.apache.org/blog/2024/11/18/datafusion-fastest-single-node-parquet-clickbench/); ClickHouse's [chDB C/C++ integration documentation](https://clickhouse.com/docs/chdb/install/c) and [Windows/WSL guidance](https://clickhouse.com/docs/resources/support-center/knowledge-base/setup-installation/install-clickhouse-windows10); SQLite's [project overview](https://sqlite.org/about.html), [public-domain declaration](https://sqlite.org/copyright.html), and Microsoft [Microsoft.Data.Sqlite native-library documentation](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/custom-versions); and Microsoft's [SQL Server Express LocalDB documentation](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17), [SQL Server 2025 edition limits](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2025?view=sql-server-ver17), and [columnstore overview](https://learn.microsoft.com/en-us/sql/relational-databases/indexes/columnstore-indexes-overview?view=sql-server-ver17). [\[103\]](https://datafusion.apache.org/?utm_source=chatgpt.com)

**Research status: August 27, 2026, America/Denver.** Areas intentionally left **unspecified** because the reviewed primary sources do not establish them include a supported DuckDB COM/WinRT/UWP/Store component, a formally documented Python-free release-equivalent MSVC source-build path, exact Windows-specific DuckDB SIMD dispatch details, an official current Windows-vs-Linux DuckDB performance comparison, chDB's native-Windows support beyond WSL, chDB's precise C-API thread-safety contract, and LocalDB-specific support for every SQL Server 2025 columnstore option. [\[104\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com)

------------------------------------------------------------------------

[\[1\]](https://duckdb.org/docs/current/dev/building/overview?utm_source=chatgpt.com) [\[7\]](https://duckdb.org/docs/current/dev/building/overview?utm_source=chatgpt.com) [\[16\]](https://duckdb.org/docs/current/dev/building/overview?utm_source=chatgpt.com) [\[28\]](https://duckdb.org/docs/current/dev/building/overview?utm_source=chatgpt.com) [\[29\]](https://duckdb.org/docs/current/dev/building/overview?utm_source=chatgpt.com) [\[87\]](https://duckdb.org/docs/current/dev/building/overview?utm_source=chatgpt.com) Building DuckDB from Source – DuckDB

<https://duckdb.org/docs/current/dev/building/overview?utm_source=chatgpt.com>

[\[2\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com) [\[4\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com) [\[11\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com) [\[14\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com) [\[17\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com) [\[18\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com) [\[21\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com) [\[26\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com) [\[27\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com) [\[42\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com) [\[95\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com) [\[99\]](https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com) Client Overview – DuckDB

<https://duckdb.org/docs/current/clients/overview?utm_source=chatgpt.com>

[\[3\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com) [\[8\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com) [\[13\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com) [\[19\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com) [\[77\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com) [\[100\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com) [\[104\]](https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com) Windows – DuckDB

<https://duckdb.org/docs/lts/dev/building/windows?utm_source=chatgpt.com>

[\[5\]](https://duckdb.org/docs/current/internals/vector?utm_source=chatgpt.com) [\[30\]](https://duckdb.org/docs/current/internals/vector?utm_source=chatgpt.com) [\[34\]](https://duckdb.org/docs/current/internals/vector?utm_source=chatgpt.com) [\[78\]](https://duckdb.org/docs/current/internals/vector?utm_source=chatgpt.com) [\[82\]](https://duckdb.org/docs/current/internals/vector?utm_source=chatgpt.com) Execution Format – DuckDB

<https://duckdb.org/docs/current/internals/vector?utm_source=chatgpt.com>

[\[6\]](https://datafusion.apache.org/?utm_source=chatgpt.com) [\[15\]](https://datafusion.apache.org/?utm_source=chatgpt.com) [\[43\]](https://datafusion.apache.org/?utm_source=chatgpt.com) [\[47\]](https://datafusion.apache.org/?utm_source=chatgpt.com) [\[70\]](https://datafusion.apache.org/?utm_source=chatgpt.com) [\[103\]](https://datafusion.apache.org/?utm_source=chatgpt.com) Apache DataFusion — Apache DataFusion documentation

<https://datafusion.apache.org/?utm_source=chatgpt.com>

[\[9\]](https://github.com/duckdb/duckdb/blob/main/src/CMakeLists.txt?utm_source=chatgpt.com) duckdb/src/CMakeLists.txt at main · duckdb/duckdb · GitHub

<https://github.com/duckdb/duckdb/blob/main/src/CMakeLists.txt?utm_source=chatgpt.com>

[\[10\]](https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com) [\[12\]](https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com) [\[20\]](https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com) [\[25\]](https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com) [\[72\]](https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com) [\[94\]](https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com) [\[101\]](https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com) Getting Started \| DuckDB.NET

<https://duckdb.net/docs/getting-started.html?utm_source=chatgpt.com>

[\[22\]](https://duckdb.org/2026/03/20/duckdb-extensionkit-csharp?utm_source=chatgpt.com) DuckDB.ExtensionKit: Building DuckDB Extensions in C# – DuckDB

<https://duckdb.org/2026/03/20/duckdb-extensionkit-csharp?utm_source=chatgpt.com>

[\[23\]](https://www.nuget.org/packages/Microsoft.DotNet.Interactive.DuckDB/1.0.0-beta.25323.1?utm_source=chatgpt.com) NuGet Gallery \| Microsoft.DotNet.Interactive.DuckDB 1.0.0-beta.25323.1

<https://www.nuget.org/packages/Microsoft.DotNet.Interactive.DuckDB/1.0.0-beta.25323.1?utm_source=chatgpt.com>

[\[24\]](https://duckdb.org/docs/current/clients/odbc/windows?utm_source=chatgpt.com) ODBC API on Windows – DuckDB

<https://duckdb.org/docs/current/clients/odbc/windows?utm_source=chatgpt.com>

[\[31\]](https://duckdb.org/docs/current/guides/performance/how_to_tune_workloads?utm_source=chatgpt.com) Tuning Workloads – DuckDB

<https://duckdb.org/docs/current/guides/performance/how_to_tune_workloads?utm_source=chatgpt.com>

[\[32\]](https://duckdb.org/2024/07/09/memory-management?utm_source=chatgpt.com) [\[84\]](https://duckdb.org/2024/07/09/memory-management?utm_source=chatgpt.com) Memory Management in DuckDB – DuckDB

<https://duckdb.org/2024/07/09/memory-management?utm_source=chatgpt.com>

[\[33\]](https://duckdb.org/docs/lts/data/overview?utm_source=chatgpt.com) Importing Data – DuckDB

<https://duckdb.org/docs/lts/data/overview?utm_source=chatgpt.com>

[\[35\]](https://duckdb.org/docs/current/internals/jemalloc?utm_source=chatgpt.com) jemalloc – DuckDB

<https://duckdb.org/docs/current/internals/jemalloc?utm_source=chatgpt.com>

[\[36\]](https://duckdb.org/docs/lts/dev/benchmark?utm_source=chatgpt.com) [\[88\]](https://duckdb.org/docs/lts/dev/benchmark?utm_source=chatgpt.com) Benchmark Suite – DuckDB

<https://duckdb.org/docs/lts/dev/benchmark?utm_source=chatgpt.com>

[\[37\]](https://duckdb.org/docs/current/connect/concurrency?utm_source=chatgpt.com) [\[39\]](https://duckdb.org/docs/current/connect/concurrency?utm_source=chatgpt.com) Concurrency – DuckDB

<https://duckdb.org/docs/current/connect/concurrency?utm_source=chatgpt.com>

[\[38\]](https://duckdb.org/docs/lts/clients/c/connect?utm_source=chatgpt.com) Startup & Shutdown – DuckDB

<https://duckdb.org/docs/lts/clients/c/connect?utm_source=chatgpt.com>

[\[40\]](https://duckdb.org/docs/current/internals/storage?utm_source=chatgpt.com) [\[81\]](https://duckdb.org/docs/current/internals/storage?utm_source=chatgpt.com) Storage Versions and Format – DuckDB

<https://duckdb.org/docs/current/internals/storage?utm_source=chatgpt.com>

[\[41\]](https://github.com/duckdb/duckdb/releases?utm_source=chatgpt.com) [\[91\]](https://github.com/duckdb/duckdb/releases?utm_source=chatgpt.com) Releases · duckdb/duckdb

<https://github.com/duckdb/duckdb/releases?utm_source=chatgpt.com>

[\[44\]](https://datafusion.apache.org/contributor-guide/development_environment.html?utm_source=chatgpt.com) [\[71\]](https://datafusion.apache.org/contributor-guide/development_environment.html?utm_source=chatgpt.com) Development Environment — Apache DataFusion documentation

<https://datafusion.apache.org/contributor-guide/development_environment.html?utm_source=chatgpt.com>

[\[45\]](https://datafusion.apache.org/blog/2024/11/18/datafusion-fastest-single-node-parquet-clickbench/?utm_source=chatgpt.com) [\[96\]](https://datafusion.apache.org/blog/2024/11/18/datafusion-fastest-single-node-parquet-clickbench/?utm_source=chatgpt.com) Apache DataFusion is now the fastest single node engine for querying Apache Parquet files - Apache DataFusion Blog

<https://datafusion.apache.org/blog/2024/11/18/datafusion-fastest-single-node-parquet-clickbench/?utm_source=chatgpt.com>

[\[46\]](https://datafusion.apache.org/_sources/user-guide/introduction.md.txt?utm_source=chatgpt.com) https://datafusion.apache.org/\_sources/user-guide/introduction.md.txt

<https://datafusion.apache.org/_sources/user-guide/introduction.md.txt?utm_source=chatgpt.com>

[\[48\]](https://clickhouse.com/docs/chdb/install?utm_source=chatgpt.com) [\[73\]](https://clickhouse.com/docs/chdb/install?utm_source=chatgpt.com) Language integrations index - ClickHouse Documentation

<https://clickhouse.com/docs/chdb/install?utm_source=chatgpt.com>

[\[49\]](https://clickhouse.com/docs/resources/support-center/knowledge-base/setup-installation/install-clickhouse-windows10?utm_source=chatgpt.com) [\[50\]](https://clickhouse.com/docs/resources/support-center/knowledge-base/setup-installation/install-clickhouse-windows10?utm_source=chatgpt.com) How do I install ClickHouse on Windows 10? - ClickHouse Documentation

<https://clickhouse.com/docs/resources/support-center/knowledge-base/setup-installation/install-clickhouse-windows10?utm_source=chatgpt.com>

[\[51\]](https://clickhouse.com/docs/chdb/install/c?utm_source=chatgpt.com) [\[52\]](https://clickhouse.com/docs/chdb/install/c?utm_source=chatgpt.com) [\[76\]](https://clickhouse.com/docs/chdb/install/c?utm_source=chatgpt.com) [\[97\]](https://clickhouse.com/docs/chdb/install/c?utm_source=chatgpt.com) chDB for C and C++ - ClickHouse Documentation

<https://clickhouse.com/docs/chdb/install/c?utm_source=chatgpt.com>

[\[53\]](https://clickhouse.com/docs/chdb?utm_source=chatgpt.com) chDB - ClickHouse Documentation

<https://clickhouse.com/docs/chdb?utm_source=chatgpt.com>

[\[54\]](https://sqlite.org/about.html?utm_source=chatgpt.com) [\[56\]](https://sqlite.org/about.html?utm_source=chatgpt.com) [\[59\]](https://sqlite.org/about.html?utm_source=chatgpt.com) [\[61\]](https://sqlite.org/about.html?utm_source=chatgpt.com) About SQLite

<https://sqlite.org/about.html?utm_source=chatgpt.com>

[\[55\]](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/custom-versions?utm_source=chatgpt.com) [\[98\]](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/custom-versions?utm_source=chatgpt.com) Custom SQLite versions - Microsoft.Data.Sqlite \| Microsoft Learn

<https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/custom-versions?utm_source=chatgpt.com>

[\[57\]](https://a1.sqlite.org/eqp.html?utm_source=chatgpt.com) [\[79\]](https://a1.sqlite.org/eqp.html?utm_source=chatgpt.com) EXPLAIN QUERY PLAN

<https://a1.sqlite.org/eqp.html?utm_source=chatgpt.com>

[\[58\]](https://sqlite.org/wal.html?utm_source=chatgpt.com) [\[85\]](https://sqlite.org/wal.html?utm_source=chatgpt.com) Write-Ahead Logging

<https://sqlite.org/wal.html?utm_source=chatgpt.com>

[\[60\]](https://sqlite.org/copyright.html?utm_source=chatgpt.com) [\[90\]](https://sqlite.org/copyright.html?utm_source=chatgpt.com) SQLite Copyright

<https://sqlite.org/copyright.html?utm_source=chatgpt.com>

[\[62\]](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17&utm_source=chatgpt.com) [\[63\]](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17&utm_source=chatgpt.com) [\[65\]](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17&utm_source=chatgpt.com) [\[68\]](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17&utm_source=chatgpt.com) SQL Server Express LocalDB - SQL Server \| Microsoft Learn

<https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17&utm_source=chatgpt.com>

[\[64\]](https://learn.microsoft.com/en-us/sql/relational-databases/indexes/columnstore-indexes-overview?view=sql-server-ver17&utm_source=chatgpt.com) [\[80\]](https://learn.microsoft.com/en-us/sql/relational-databases/indexes/columnstore-indexes-overview?view=sql-server-ver17&utm_source=chatgpt.com) [\[89\]](https://learn.microsoft.com/en-us/sql/relational-databases/indexes/columnstore-indexes-overview?view=sql-server-ver17&utm_source=chatgpt.com) Columnstore indexes: Overview - SQL Server \| Microsoft Learn

<https://learn.microsoft.com/en-us/sql/relational-databases/indexes/columnstore-indexes-overview?view=sql-server-ver17&utm_source=chatgpt.com>

[\[66\]](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2025?view=sql-server-ver17&utm_source=chatgpt.com) [\[83\]](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2025?view=sql-server-ver17&utm_source=chatgpt.com) [\[92\]](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2025?view=sql-server-ver17&utm_source=chatgpt.com) Editions and Supported Features of SQL Server 2025 - SQL Server \| Microsoft Learn

<https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2025?view=sql-server-ver17&utm_source=chatgpt.com>

[\[67\]](https://www.microsoft.com/en-us/sql-server/sql-server-downloads?utm_source=chatgpt.com) [\[74\]](https://www.microsoft.com/en-us/sql-server/sql-server-downloads?utm_source=chatgpt.com) SQL Server Downloads \| Microsoft

<https://www.microsoft.com/en-us/sql-server/sql-server-downloads?utm_source=chatgpt.com>

[\[69\]](https://duckdb.org/docs/current/clients/c/overview?utm_source=chatgpt.com) [\[75\]](https://duckdb.org/docs/current/clients/c/overview?utm_source=chatgpt.com) [\[93\]](https://duckdb.org/docs/current/clients/c/overview?utm_source=chatgpt.com) Overview – DuckDB

<https://duckdb.org/docs/current/clients/c/overview?utm_source=chatgpt.com>

[\[86\]](https://duckdb.org/2026/08/20/duckdb-20-peg-parser?utm_source=chatgpt.com) DuckDB v2.0: Your Database Deserves a Better Parser – DuckDB

<https://duckdb.org/2026/08/20/duckdb-20-peg-parser?utm_source=chatgpt.com>

[\[102\]](https://duckdb.org/2026/08/17/duckdb-20-highlights?utm_source=chatgpt.com) A Preview of DuckDB v2.0 – DuckDB

<https://duckdb.org/2026/08/17/duckdb-20-highlights?utm_source=chatgpt.com>
