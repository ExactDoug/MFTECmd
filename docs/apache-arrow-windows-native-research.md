# Apache Arrow on Windows: Native Microsoft-Platform Support, Gaps, and Alternatives

## Executive summary

**Bottom line: Apache Arrow does have bona fide Windows-native implementations that can be embedded directly into ordinary Microsoft applications without Python. What does *not* exist is a separate Microsoft-owned, Windows-first Arrow fork or Windows-specific implementation.** The upstream Apache project itself supplies the relevant implementations:

| Question | Finding |
|---|---|
| Native C++/Win32? | **Yes.** Arrow C++ builds with CMake and MSVC/Visual Studio, produces ordinary native libraries, supports static or DLL linkage, and is available through vcpkg. citeturn20search0turn20search1turn23search1 |
| Native C#/.NET? | **Yes.** `Apache.Arrow` is an official Apache C# implementation distributed on NuGet, with no Python or Arrow C++ runtime requirement for the core package. It targets .NET Standard 2.0, .NET 6, .NET 8 and .NET Framework 4.6.2. citeturn18search0turn18search1 |
| MSBuild? | **Yes, indirectly/directly depending on language.** C++ uses CMake's Visual Studio generator, which generates a Visual Studio/MSBuild build; .NET uses normal SDK-style .NET/MSBuild tooling (`dotnet build`). Arrow does not use a Windows-only `.vcxproj` build as its canonical C++ build system. citeturn20search0turn2search6 |
| vcpkg? | **Yes.** Arrow C++ 25.0.1 is in Microsoft's vcpkg, including feature-selectable Compute, Acero, Dataset, Flight, Parquet, S3, GCS and allocator support. Apache describes that port as maintained by Microsoft team members plus community contributors, although third-party package-manager artifacts are not formally Apache release artifacts. citeturn23search0turn23search1 |
| NuGet? | **Yes.** Official Apache packages exist for core Arrow, compression and Flight-related .NET functionality. citeturn23search0turn18search1turn18search2 |
| MSIX? | **No Arrow-provided MSIX SDK/package was found.** Apache's current installation matrix lists NuGet, vcpkg, Conan, MSYS2, conda and other channels, but no MSIX distribution. An application can of course place Arrow assemblies/DLLs inside its own MSIX. citeturn23search0 |
| UWP? | **Possible for the managed library, but not a first-class Arrow target.** `Apache.Arrow` targets .NET Standard 2.0, and Microsoft says UWP Build 16299+ implements .NET Standard 2.0. Apache does not document or certify UWP specifically. Full Arrow C++ should not be assumed UWP-compatible without dependency-by-dependency validation. citeturn18search0turn23search6turn23search8 |
| Windows-specific optimization? | **Some Windows adaptation, but mostly cross-platform optimization rather than Windows-specialized engineering.** Arrow has runtime x86 SIMD dispatch, aligned buffers, mimalloc/jemalloc memory pools, mmap, parallel I/O, MSVC-specific build fixes and Windows compatibility paths. It does not document a Windows-specific IOCP/I/O-ring/DirectStorage-style backend or Windows scheduler integration. citeturn21search0turn21search1turn21search2turn23search1 |
| Windows vs Linux benchmark? | **No authoritative apples-to-apples Arrow benchmark was found.** Arrow provides an extensive benchmark harness, but its documented benchmark/Conbench infrastructure is predominantly Linux-based rather than a published matched Windows-vs-Linux comparison. citeturn14search0turn14search7turn14search9 |

The most important architectural distinction is therefore:

> **Arrow on Windows is Windows-native, but not Windows-first.**

For a **C#/desktop Microsoft application whose principal requirement is interoperable Arrow memory and IPC**, the official `Apache.Arrow` NuGet package is the cleanest choice. For a **high-performance Win32/C++ analytical engine**, upstream Arrow C++ built through vcpkg/CMake/MSVC is the most complete option. For a **small C ABI or plug-in boundary**, Apache **nanoarrow** is especially attractive because its C runtime is only a few hundred kilobytes and can be vendored as essentially `nanoarrow.c` + `nanoarrow.h`. citeturn18search0turn20search1turn17search0

If the actual requirement is **local analytical querying rather than Arrow-format interchange**, **DuckDB** is the strongest alternative: it is an embedded native analytical database with a columnar-vectorized engine, an official Windows DLL, C API and excellent Arrow/ADBC interoperability. It is not, however, a substitute for Arrow's standardized in-memory buffer ABI. citeturn22search1turn16search0turn22search8

## What Apache Arrow actually provides on Windows

As of **August 27, 2026**, the current main Apache Arrow release is **25.0.1, released August 10, 2026**. It was published by the Apache Arrow PMC as a bug-fix release containing nine resolved issues across ten commits from six contributors. The project is governed and maintained by the Apache Arrow community/PMC, not Microsoft. citeturn17search11turn23search0

The project's official repository is [apache/arrow](https://github.com/apache/arrow), with current documentation at [arrow.apache.org/docs](https://arrow.apache.org/docs/) and installation information at [arrow.apache.org/install](https://arrow.apache.org/install/). citeturn23search0turn20search1

### Arrow C++ is a real native Windows library

The C++ implementation is not a wrapper around Python. Apache explicitly documents Windows development with **Visual Studio/MSVC**, Ninja, NMake, MSYS2, vcpkg and Windows-on-ARM64 experimentation. The documentation describes Windows as one of the platforms on which the project has worked to make CMake builds operate "out of the box" for a substantial subset of Arrow. citeturn20search0turn20search8

A conventional MSVC source build looks like:

```bat
cd arrow\cpp
mkdir build
cd build

cmake .. ^
  -G "Visual Studio 17 2022" ^
  -A x64 ^
  -DARROW_BUILD_TESTS=ON

cmake --build . --config Release
```

Apache's documentation shows the same procedure with Visual Studio generators and explicitly says newer Visual Studio versions can be selected by changing the generator. citeturn20search0

For consumption from another CMake/MSVC application, Arrow's supported model is equally conventional:

```cmake
cmake_minimum_required(VERSION 3.25)
project(MyWindowsApp LANGUAGES CXX)

find_package(Arrow REQUIRED)

add_executable(MyWindowsApp main.cpp)
target_link_libraries(MyWindowsApp PRIVATE Arrow::arrow_shared)
```

`Arrow::arrow_shared` and `Arrow::arrow_static` are official imported CMake targets; analogous CMake packages exist for Arrow Compute, Dataset, Flight, Parquet, Acero and other components. citeturn20search1

This means that from a Win32 application's perspective, Arrow is simply another native C++ dependency. There is no Python interpreter, Java VM, Node runtime or IPC proxy involved unless the application itself chooses to add one. The normal concerns are those of a large native C++ dependency graph: ABI/compiler consistency, CRT selection and static-versus-dynamic linkage of third-party libraries. Apache explicitly warns that mixing dependency versions or static/dynamic linkage models can cause ODR violations and crashes. citeturn20search1

### vcpkg gives it unusually good Microsoft-toolchain integration

Microsoft's vcpkg currently contains **Arrow 25.0.1**. The port exposes features including Compute, Acero, CSV, CUDA, Dataset, filesystem support, Flight, Flight SQL, GCS, JSON, ORC, Parquet, S3, jemalloc and mimalloc. Its dependency graph includes libraries such as Boost, Brotli, bzip2, gflags, LZ4, OpenSSL, RE2, Snappy, Thrift, utf8proc, xsimd, zlib and zstd. citeturn23search1turn23search3

A normal Visual Studio/vcpkg project can therefore start with:

```bat
vcpkg add port arrow
```

or classic mode:

```bat
vcpkg install arrow
```

Apache's own Windows development instructions additionally show its source-tree vcpkg manifest workflow:

```bat
vcpkg install ^
  --triplet x64-windows ^
  --x-manifest-root cpp ^
  --feature-flags=versions ^
  --clean-after-build
```

`x64-windows` produces dynamic dependencies by default; `x64-windows-static` selects static linkage. citeturn20search0

An important governance nuance is that Apache labels package-manager builds in its "Other Installers" section as technically **unofficial Apache releases**, because the PMC does not vote on each binary package. At the same time, Apache specifically says the Arrow vcpkg port is kept current by **Microsoft team members and community contributors**. That is probably the strongest basis for describing Arrow's Windows C++ support as *Microsoft-tooling-first-class*, even though Microsoft does not own the implementation. citeturn23search0

The vcpkg port itself contains Windows/MSVC-specific patches, including `0001-msvc-static-name.patch`, `0005-cmake-msvcruntime.patch` and a vcpkg mimalloc integration patch, which demonstrates actual Windows toolchain maintenance rather than merely "it happens to compile on Windows." citeturn23search1

### Apache Arrow .NET is a separate official implementation

The managed implementation is now maintained in the official [apache/arrow-dotnet](https://github.com/apache/arrow-dotnet) repository. Apache describes it directly as "**An implementation of Arrow targeting .NET**." The C++ monorepo and .NET implementation now have separate release cadences, which is why the current C++ release is 25.0.1 while the current `Apache.Arrow` NuGet package is **23.0.0**, released on NuGet on **May 6, 2026**. citeturn18search0turn18search7turn1search0

Current framework targets are:

**C# 11; .NET Standard 2.0; .NET 6.0; .NET 8.0; and .NET Framework 4.6.2.** citeturn18search0turn18search1

Installation is ordinary NuGet/MSBuild:

```bat
dotnet add package Apache.Arrow --version 23.0.0
dotnet build
```

or:

```xml
<PackageReference Include="Apache.Arrow" Version="23.0.0" />
```

The repository itself uses normal .NET tooling (`dotnet build`, `dotnet test`, `dotnet pack`), so this is a fully conventional Visual Studio/.NET dependency rather than a P/Invoke layer around Arrow C++. citeturn18search1turn2search6

The implementation supports Arrow arrays, schemas and record batches; Arrow IPC files and streams; asynchronous I/O; and optional compression. It uses `Span<T>`, `Memory<T>`, `MemoryManager<T>` and `System.Buffers`, with 64-byte-aligned Arrow allocations. citeturn18search0turn18search3

There are significant capability differences from Arrow C++, however. Apache's own feature page currently lists tensors, true large arrays above 2 GiB and some newer Arrow types/features as incomplete or unimplemented, and the .NET implementation currently lacks the C++ implementation's full generic compute/kernel layer. This makes it an excellent **Arrow interoperability and serialization library**, but not a feature-for-feature C# port of Arrow C++/Acero. citeturn18search0turn7view2

Compression is deliberately separated into `Apache.Arrow.Compression`; Flight and server support have separate `Apache.Arrow.Flight` and `Apache.Arrow.Flight.AspNetCore` packages. Flight 23.0.0 pulls the normal .NET gRPC and Protocol Buffers dependencies rather than Python components. citeturn23search0turn18search2

### UWP is a compatibility case, not a supported Arrow SKU

There is no dedicated `Apache.Arrow.UWP`, WinRT projection or UWP-specific Arrow SDK in the Apache distribution. Nevertheless, `Apache.Arrow` targets **.NET Standard 2.0**, while Microsoft states that **UWP 10.0.16299 and later supports .NET Standard 2.0** and that UWP projects using it must set Build 16299 or later as their minimum version. citeturn18search0turn23search6turn23search8

Therefore:

**Managed UWP:** technically plausible from the target-framework perspective, but it should be treated as an application qualification exercise rather than an Apache-guaranteed deployment configuration. Store restrictions, AOT behavior and particular dependency APIs still need validation. citeturn23search8turn18search0

**Native C++ UWP:** not something Apache's Windows documentation advertises as supported. Arrow's large dependency graph means conventional Win32/Desktop MSVC should be treated as the supported Windows baseline unless every selected vcpkg dependency has been validated for a UWP triplet. citeturn20search0turn23search1

By contrast, ordinary **Win32 desktop applications** are directly in Arrow C++'s normal MSVC build/usage model. citeturn20search0turn20search1

The relationship between the main options is:

```mermaid
flowchart TD
    F["Apache Arrow columnar format / IPC / C Data Interface"]

    F --> CPP["Apache Arrow C++ 25.0.1"]
    F --> NET["Apache Arrow .NET 23.0.0"]
    F --> NANO["Apache nanoarrow 0.9.0"]

    CPP --> VC["CMake + MSVC / Visual Studio"]
    CPP --> VCPKG["vcpkg"]
    VC --> WIN32["Win32 / native Microsoft application"]

    NET --> NUGET["NuGet + MSBuild"]
    NUGET --> DOTNET[".NET desktop / service"]
    NUGET --> UWP["UWP 16299+ potentially\nvia .NET Standard 2.0"]

    NANO --> CABI["C / C++ ABI"]
    CABI --> EMBED["Small embedded/native component"]
```

The versions and packaging paths above reflect the current Apache, NuGet and vcpkg releases as of August 2026. citeturn23search0turn23search1turn18search1turn17search3

## Windows optimization, dependencies, and deployment characteristics

### SIMD and CPU execution

Arrow C++ has genuine hardware-oriented optimization, but it is **not Windows-specific**. On x86 it detects CPU capabilities at runtime and selects optimized code paths; the runtime SIMD ceiling can include SSE-family, AVX, AVX2 and AVX-512 paths, with `ARROW_USER_SIMD_LEVEL` available to restrict dispatch. Apache describes the default behavior as detecting the current CPU and choosing the best available execution path. citeturn21search0turn21search8

Arrow 25 also continued work on runtime dispatch beyond x86: the 25.0.0 release moved CPU capability detection to xsimd and added dynamic SVE dispatch for ARM64. That reinforces that SIMD engineering is architecture-oriented rather than a Windows-only optimization layer. citeturn1search0

On Windows/x86/x64 this still matters substantially: the same AVX2/AVX-512-optimized kernels can execute in a native MSVC-built process when compiled with the appropriate Arrow features. The vcpkg port includes `xsimd` as a dependency. citeturn21search0turn23search1

Windows-on-ARM is currently less mature. Apache documents ARM64 Windows builds through **Ninja + `clang-cl`**, while stating that MSVC could not yet be used for the Windows/ARM64 build because of compatibility issues in dependencies such as xsimd and Boost; Apache labels that configuration experimental and not as comprehensively exercised in CI. citeturn20search0

### Memory allocation

The native implementation follows Arrow's performance-oriented memory rules: buffers allocated through the Arrow C++ API are **64-byte aligned and padded** according to the memory specification. Its default `MemoryPool` selects mimalloc if compiled in, otherwise jemalloc if compiled in, otherwise the system allocator. Applications can override the choice through `ARROW_DEFAULT_MEMORY_POOL` and can supply custom memory pools. citeturn20search3turn21search10turn21search0

This is relevant to Windows because vcpkg exposes both `mimalloc` and `jemalloc` Arrow features and carries a vcpkg-specific mimalloc integration patch. It is nevertheless more accurate to call this **Windows-friendly allocator support** than a Windows-specialized allocator architecture: Arrow uses the same memory-pool abstraction cross-platform. citeturn23search1

The .NET implementation likewise maintains 64-byte alignment, but allocation is integrated with managed .NET memory abstractions. Apache warns that the default over-allocation strategy can be disproportionately expensive for very small buffers—a one-byte logical buffer may require up to a 64-byte backing allocation to satisfy alignment. citeturn18search0

### I/O and kernel integration

Arrow C++ has capable native I/O primitives: sequential streams, random-access files, parallel `ReadAt`, buffered and compressed I/O, memory-mapped files and zero-copy mmap reads. The filesystem abstraction supports local storage plus build-selectable cloud/filesystem backends. citeturn21search2turn21search5

The execution model uses separate CPU and I/O thread pools. On Linux, the default CPU pool can use process CPU affinity; on other systems it falls back to `std::thread::hardware_concurrency`. The I/O pool defaults to eight threads and is configurable through `ARROW_IO_THREADS` or the C++ API. citeturn21search1turn21search4

That detail is instructive: **Arrow is not using a documented Windows-specialized scheduler here.** I found no current Apache documentation describing an Arrow backend built around Windows IOCP, Windows Thread Pool APIs, Registered I/O, DirectStorage or a comparable Windows-specific kernel fast path. Instead, Arrow presents cross-platform C++ file/stream and threading abstractions with Windows compatibility code underneath. The absence of such a documented mechanism should be understood as a research finding, not proof that no individual internal system call is Windows-specific. citeturn21search1turn21search2

There *are* Windows-specific compatibility accommodations. For example, Arrow exposes Windows-specific timezone configuration in some Clang/libc++ configurations, and the vcpkg port carries explicit MSVC runtime/static-link naming fixes. These are platform-enablement measures rather than performance engines. citeturn20search2turn23search1

### Runtime footprint

For a **full shared Arrow C++ build**, application deployment may include Arrow DLLs plus DLLs for whichever third-party features were enabled. The vcpkg dependency list illustrates why a "build everything" Arrow distribution is considerably heavier than a simple serialization library. A static vcpkg triplet can consolidate much of this into the application's binaries, at the cost of binary size and the need to keep static-link compile definitions/configuration consistent. citeturn23search1turn20search0turn20search1

For **Apache.Arrow .NET**, the core runtime environment is the targeted .NET implementation plus normal managed NuGet dependencies; it does not require the Apache Arrow C++ DLL or Python. Adding Flight introduces gRPC/Protobuf dependencies, while IPC compression is a separately installed package. citeturn18search0turn18search1turn18search2

For minimal native embedding, **nanoarrow** is dramatically smaller: Apache says its C runtime compiles to only a few hundred kilobytes and distributes the basic C library as two files. It handles Arrow C Data, C Stream, C Device and Arrow IPC rather than reproducing Arrow C++'s entire analytical engine. citeturn17search0turn17search8

## Packaging, build systems, and support matrix

The practical support picture is:

| Microsoft scenario | Best-supported path | Assessment |
|---|---|---|
| C#/.NET desktop or service | `Apache.Arrow` NuGet | **First-class.** Official Apache NuGet package and normal MSBuild/.NET project integration. citeturn18search0turn18search1 |
| C++/MSVC Win32 | Arrow C++ + CMake + vcpkg | **First-class native build path.** Apache explicitly documents Visual Studio/MSVC and Microsoft vcpkg. citeturn20search0turn23search0 |
| Visual Studio/MSBuild C++ project | Consume a CMake/vcpkg Arrow build from VS/MSBuild | **Good, but CMake-centric.** CMake is Arrow's supported integration layer; Visual Studio's generator supplies the MSBuild solution/project layer. citeturn20search0turn20search1 |
| UWP C# | `.NET Standard 2.0` asset | **Framework-compatible in principle for Build 16299+, not explicitly Arrow-certified.** citeturn18search0turn23search8 |
| UWP C++ | No documented supported configuration | **Weak/uncertain.** Do not assume the full dependency graph is Store/UWP-compatible. citeturn20search0turn23search1 |
| MSIX | Bundle the selected NuGet assemblies or native binaries in your app | **No Arrow-specific MSIX package.** Apache's current installer list contains no MSIX distribution. citeturn23search0 |
| Small Win32 C/C++ component | nanoarrow | **Very attractive** for Arrow interchange when full Compute/Acero/Dataset functionality is unnecessary. citeturn17search0turn17search12 |
| Windows ARM64 | Arrow C++ via Clang/Ninja; .NET subject to .NET runtime support | **Native C++ support is still explicitly experimental in Apache's Windows documentation, and MSVC is not the documented compiler path.** citeturn20search0 |

### C++ source-build dependency workflow

Apache's source repository can use its own vcpkg manifest rather than manually locating Boost and the compression/networking libraries. Arrow's Windows instructions explicitly support this arrangement and also allow static or shared dependency selection. citeturn20search0

A reasonable Microsoft-oriented build pipeline is therefore:

```text
Visual Studio 2022
       |
       +-- CMake
       |     |
       |     +-- vcpkg toolchain / Arrow manifest
       |     |
       |     +-- Arrow C++ + selected features
       |
       +-- MSVC x64
       |
       +-- Your Win32 application
```

Once installed, the supported downstream CMake interface is `find_package(Arrow REQUIRED)` plus one or more imported targets such as `Arrow::arrow_shared`, `ArrowCompute::arrow_compute_shared` or their static counterparts. citeturn20search1

### .NET build and package split

For a typical application:

```bat
dotnet add package Apache.Arrow --version 23.0.0
```

For compressed Arrow IPC:

```bat
dotnet add package Apache.Arrow.Compression --version 23.0.0
```

For Arrow Flight:

```bat
dotnet add package Apache.Arrow.Flight --version 23.0.0
```

The Apache installation page lists the core, compression, Flight and Flight ASP.NET Core packages as Apache-provided NuGet distributions. citeturn23search0turn18search2

One architectural caveat is critical: installing `Apache.Arrow` does **not** give C# applications the full set of Arrow C++ compute kernels or Acero execution nodes. A C# application that wants the Arrow memory model **plus heavyweight local analytics** will often either use Microsoft.Data.Analysis above Arrow, invoke a native Arrow C++ component, or use an embedded engine such as DuckDB. citeturn18search0turn21search6

## Performance evidence on Windows versus Linux

There is strong evidence that Arrow is designed for performance on both systems, but **I did not find a current Apache-published matched-hardware Windows-vs-Linux benchmark from which a credible percentage difference could be quoted**.

Apache's C++ benchmark tooling is extensive. `archery benchmark run` executes the microbenchmark suites; `archery benchmark diff` compares builds/commits; custom CMake flags can be supplied to test matters such as SIMD configuration; and results can be exported to JSON. citeturn14search0turn14search3

That means an organization can perform a proper comparison along these lines:

```bat
archery benchmark run C:\arrow\cpp\release-build --output=windows.json
```

and on a matched Linux environment:

```bash
archery benchmark run /opt/arrow/cpp/release-build --output=linux.json
```

The two result sets should only be treated as an operating-system comparison when CPU model, memory configuration, Arrow commit, optimization level, allocator, SIMD ceiling, feature selection, compiler versions and benchmark data are controlled. Arrow's benchmark documentation itself emphasizes benchmark comparison and statistical regression detection rather than publishing a universal platform ranking. citeturn14search0turn14search6

The project's historical/current Conbench setup reinforces the lack of a ready-made Windows comparison. Its benchmark-environment documentation gives Ubuntu-based benchmark setup instructions, and a recent 2026 Apache Arrow Conbench report identifies performance hosts such as `amd64-c6a-4xlarge-linux` and `arm64-t4g-2xlarge-linux`, even though the broader CI workflow also runs ordinary Windows/MSVC jobs. In other words, Windows is tested as a supported build platform, but the visible standardized performance infrastructure is not an equivalent Windows/Linux benchmark matrix. citeturn14search7turn14search9

### What can reasonably be expected

There is no technical reason to expect Arrow's basic memory layout to become inherently slower merely because it is hosted in Windows: the Arrow buffers, 64-byte alignment and x86 SIMD dispatch model remain the same. But meaningful differences can arise from the compiler, allocator, filesystem implementation, thread scheduling and libraries selected at build time. This is an inference from Arrow's documented architecture rather than a claimed benchmark result. citeturn21search0turn21search1turn21search2turn21search10

One particularly relevant cross-platform difference is documented explicitly: Arrow can size its CPU thread pool from process CPU affinity on Linux, whereas the fallback used elsewhere is `std::thread::hardware_concurrency`. That does not imply Linux is faster, but it is a concrete example of why identical source code is not necessarily identically tuned by default across operating systems. citeturn21search1

For a performance-sensitive Windows product, I would therefore treat **"benchmark on our actual deployment machine" as mandatory** rather than importing Linux performance numbers.

## Windows-native alternatives and adjacent implementations

There is an important finding here: **there is no convincing Microsoft-specific non-Arrow format that provides the same combination of standardized, language-independent in-memory columnar buffers, zero-copy interchange and broad ecosystem support.**

The most credible Windows alternatives either:

1. remain implementations of the **Arrow format** itself, such as nanoarrow; or
2. solve the **analytics** side of the problem but use Arrow for interchange, such as DuckDB or Microsoft.Data.Analysis. citeturn17search0turn22search8turn15search4

### Comparison of practical candidates

| name | platform | language | packaging | Windows-optimization notes | performance | maturity | license |
|---|---|---|---|---|---|---|---|
| **[Apache Arrow C++](https://github.com/apache/arrow)** | Windows, Linux, macOS; Win32 desktop is a documented Windows path | C++ | vcpkg, Conan, MSYS2, source/CMake; no Arrow MSIX | MSVC/VS CMake builds, vcpkg MSVC patches; runtime x86 SIMD; mimalloc/jemalloc; mmap and threaded I/O. Optimizations are primarily cross-platform rather than Windows-only. citeturn20search0turn21search0turn23search1 | Full Compute/Acero engine; official benchmark suites. No credible official matched Windows/Linux result found. citeturn21search6turn14search0 | **Very high.** Flagship Arrow implementation; current 25.0.1 released Aug. 10, 2026. citeturn17search11 | Apache-2.0. citeturn23search1 |
| **[Apache Arrow .NET](https://github.com/apache/arrow-dotnet)** | .NET on Windows and other .NET platforms; UWP theoretically via netstandard2.0/Build 16299+ | C# 11 | Official NuGet; normal MSBuild/.NET; no Arrow MSIX | Uses `Span`, `Memory`, 64-byte alignment and async I/O; no documented Windows-only SIMD/kernel layer. citeturn18search0 | Strong IPC/interchange; much less compute capability than C++ and no official Windows/Linux benchmark found. citeturn18search0turn7view2 | **High for interchange, medium for analytics completeness.** Current NuGet 23.0.0, May 6, 2026. citeturn18search7 | Apache-2.0. |
| **[Apache nanoarrow](https://github.com/apache/arrow-nanoarrow)** | Portable native C/C++; suitable for Windows native embedding | C with C++ usability | Vendored `.c/.h`, CMake, vcpkg; current Apache release 0.9.0 while vcpkg currently shows 0.8.0#2 | No Windows-specialized compute engine; extremely small dependency/ABI footprint makes it attractive for DLL/plugin boundaries. citeturn17search0turn23search5 | Designed for low-overhead Arrow C Data/C Stream/IPC consumption rather than analytical kernels; C runtime is only a few hundred KB. citeturn17search0 | **Medium-high and focused.** 0.9.0 released Aug. 14, 2026; 38 resolved issues from five contributors. citeturn17search3 | Apache-2.0. citeturn23search5 |
| **[Microsoft.Data.Analysis](https://www.nuget.org/packages/Microsoft.Data.Analysis/)** | .NET, particularly natural in Microsoft applications | C# | Microsoft-prefixed NuGet package; MSBuild/PackageReference | Managed .NET implementation; no special Windows kernel backend. Crucially, package itself depends on `Apache.Arrow`. citeturn15search1 | Provides DataFrame sort/filter/group/join/elementwise operations and can wrap an Arrow `RecordBatch` without copying. No authoritative OS comparison located. citeturn15search4 | **Medium.** Current stable package is 0.23.0; still 0.x and documented in the ML.NET API surface. citeturn15search1turn15search0 | MIT via `dotnet/machinelearning`. citeturn15search2 |
| **[DuckDB](https://duckdb.org/)** | Native Windows, Linux, macOS; official Windows DLL | C API; internal C++ API; community .NET provider | Official downloadable Windows native library; vcpkg; community `DuckDB.NET.*` NuGets | Visual Studio is DuckDB's recommended Windows compiler; requires MSVC Redistributable for official Windows native build. vcpkg explicitly marks its current DuckDB port `!uwp`. citeturn16search0turn16search2 | Columnar-vectorized SQL engine; strong analytics orientation; zero-copy Arrow ADBC integration. Official benchmark suite/TPC tests, but not a replacement Arrow memory ABI. citeturn22search1turn22search8turn16search4 | **Very high.** DuckDB describes itself as a stable/mature system with millions of CI test queries; current docs identify 1.5.5 as stable. citeturn22search1turn16search1 | MIT, DuckDB Foundation. citeturn22search2turn22search1 |

### Apache nanoarrow

Nanoarrow deserves special attention because it answers a slightly different version of the original question: *"What is the smallest truly native Arrow component I can compile directly into a Windows application?"*

Apache describes nanoarrow as helpers for **Arrow C Data, C Stream, C Device and serialized Arrow IPC**. Its C runtime compiles into a few hundred kilobytes, and the core is distributable as `nanoarrow.c` and `nanoarrow.h`. CMake is its officially supported development/build system. citeturn17search0turn17search8

A basic source build is simply:

```bat
mkdir build
cd build
cmake ..
cmake --build .
```

Apache also documents CMake `FetchContent` and vendoring/bundling approaches. citeturn17search0

Version **0.9.0 was released August 14, 2026**, adding, among other things, dictionary decoding to its IPC reader and reference-counted array/buffer support. The current vcpkg port is still **0.8.0#2**, so a project requiring 0.9.0 immediately should vendor/build Apache's release rather than assume vcpkg has caught up. citeturn17search3turn23search5

Its Arrow API parity is **high for the C interchange structures and increasingly strong for IPC, but intentionally low for compute/Acero/Dataset functionality**. That is a feature, not a defect, when the requirement is merely moving Arrow-compatible columnar buffers through a native Windows API boundary. citeturn17search0

For a Win32 DLL, COM component, native extension or C ABI shared between C# and C++, nanoarrow may actually be a better engineering fit than full Arrow C++.

### Microsoft.Data.Analysis

`Microsoft.Data.Analysis` is the closest Microsoft ecosystem library to a native managed DataFrame abstraction. Its `DataFrame` supports indexing, elementwise operations, filtering, grouping, joins and other tabular operations and implements ML.NET's `IDataView` interface. citeturn15search0turn15search4

But it is **not an independent competitor to Arrow as an interchange standard**. Its current NuGet package explicitly depends on `Apache.Arrow >= 14.0.2`, and the API can create a DataFrame around an `Apache.Arrow.RecordBatch` **without copying**. Its `ArrowStringDataFrameColumn` exposes buffers in Arrow format. citeturn15search1turn15search4turn15search8

So its API parity is:

- **Analytics/DataFrame operations:** better/more convenient than bare `Apache.Arrow` C# for many common transformations. citeturn15search4
- **Arrow IPC and cross-language data interchange:** not a replacement; it delegates the Arrow aspect to Apache.Arrow. citeturn15search1turn15search4
- **Windows specialization:** essentially none; its strength is native integration with the .NET/ML.NET ecosystem rather than Windows-specific CPU or kernel facilities. citeturn15search3turn15search4

The current package is **0.23.0**, targets .NET 8 and .NET Standard 2.0, and is part of the MIT-licensed `dotnet/machinelearning` codebase. citeturn15search1turn15search2

I would therefore use Microsoft.Data.Analysis **on top of Arrow**, not instead of it, when a C# application needs convenient local DataFrame manipulation.

### DuckDB

DuckDB is a much stronger alternative where "the same primary use case" means **fast local columnar analytics**, but a weaker alternative where it means **a standardized data interchange memory representation**.

DuckDB is an embedded, in-process analytical database. Its engine uses **columnar-vectorized execution**, processing batches of values together to reduce per-value overhead; the DuckDB Foundation specifically positions this design for OLAP workloads. citeturn22search1

Windows support is unequivocally native. DuckDB's documentation recommends the **Visual Studio compiler** on Windows, uses CMake directly, and distributes native Windows libraries; the official Windows build requires the Microsoft Visual C++ Redistributable at runtime. citeturn16search0

For application ABI stability, DuckDB recommends its **C API** rather than the C++ API because the latter is explicitly described as internal and not guaranteed stable. citeturn16search1

Arrow interoperability is unusually good. The DuckDB ADBC driver transfers query results using Arrow and documents **zero-copy integration between DuckDB and Arrow**. A Windows application can therefore use DuckDB as its local analytical execution engine while retaining Arrow as its interchange boundary. citeturn22search8

The .NET situation deserves qualification. `DuckDB.NET.Data` is a mature-looking ADO.NET provider on NuGet and version 1.5.5 added Arrow result streaming, but it is a **community package owned by Giorgi Dalakishvili**, not the official DuckDB Foundation Windows/.NET binding. Its package explicitly says it does not itself contain the native DuckDB library. citeturn16search3

The current Microsoft vcpkg DuckDB port is also behind the newest upstream release—vcpkg lists **1.4.4#1**—and explicitly declares `!uwp`, which makes DuckDB a poor choice for a hard UWP requirement. citeturn16search2

For desktop Win32 applications, however, **Arrow + DuckDB** is an excellent combination: Arrow remains the standardized buffer/IPC representation and DuckDB supplies the high-level vectorized SQL engine. citeturn22search8turn22search1

## Recommended Microsoft-app integration choices

### For a C# or modern .NET application

Use **official `Apache.Arrow` from NuGet as the canonical interchange layer**.

This provides the cleanest zero-Python Microsoft development experience: NuGet/PackageReference, Visual Studio/MSBuild, managed arrays and IPC, async stream support and no dependency on the Arrow C++ DLL for basic operation. citeturn18search0turn18search1

Add `Apache.Arrow.Compression` only when compressed IPC is needed, and the Flight packages when network transport is needed. citeturn23search0turn18search2

When significant DataFrame-style C# transformations are needed, layer **Microsoft.Data.Analysis** over it. The zero-copy `FromArrowRecordBatch` bridge makes this a coherent architecture rather than two competing memory models. citeturn15search4

A sensible architecture is:

```mermaid
flowchart LR
    IO["Files / network / database"]
    AR["Apache.Arrow\nNuGet"]
    DF["Microsoft.Data.Analysis\noptional"]
    APP["C# application"]
    FL["Arrow Flight\noptional"]

    IO --> AR
    AR <--> DF
    AR --> APP
    DF --> APP
    AR <--> FL
```

This is the most Microsoft-native stack available while preserving genuine Arrow interoperability. citeturn18search0turn15search1turn15search4

### For a high-performance native C++/Win32 application

Use **Arrow C++ + vcpkg + CMake + MSVC x64**.

That is the path with the fullest Arrow functionality: Arrow arrays and IPC plus Compute, Dataset, Acero, Parquet, filesystem backends and Flight as required. Visual Studio builds are directly documented by Apache, while vcpkg provides the best Microsoft ecosystem dependency management. citeturn20search0turn20search1turn23search1

Prefer a **feature-minimized manifest** rather than enabling everything. Arrow's native dependency graph grows substantially when Flight, cloud filesystems, Parquet, compression codecs and other components are enabled. citeturn23search1

For shipping:

- Use `x64-windows` when DLL deployment is acceptable.
- Consider `x64-windows-static` when a self-contained binary is operationally preferable.
- Keep all Arrow/dependency CRT and linkage choices consistent.
- Benchmark mimalloc versus the default/system allocator in the actual application rather than assuming one allocator wins on Windows. citeturn20search0turn20search1turn21search10

### For a small native DLL, COM boundary, plug-in, or SDK

Use **Apache nanoarrow**, particularly when the objective is to exchange Arrow arrays rather than run the Arrow C++ compute engine.

Its tiny C footprint, stable C-oriented interfaces and ability to be vendored directly avoid the large transitive dependency tree of Arrow C++. It still speaks the canonical Arrow C Data/C Stream structures and IPC. citeturn17search0turn17search12

This is arguably the closest thing to a **minimal Windows-native Arrow runtime**, although Apache intentionally designs it as portable C rather than a Windows-specific library. Current projects should note the temporary package-version gap between upstream nanoarrow 0.9.0 and vcpkg 0.8.0#2. citeturn17search3turn23search5

### For an analytics-heavy desktop application

Use **Arrow as the interchange model and DuckDB as the analytical execution engine**.

This avoids reimplementing joins, grouping, aggregation and SQL planning in the application while retaining Arrow-compatible zero-copy boundaries. DuckDB's native Windows DLL and C interface make it an ordinary embedded Win32 dependency rather than a server or Python runtime. citeturn16search0turn22search1turn22search8

For C#, the community `DuckDB.NET` bindings can make this convenient, but applications with strict supply-chain/support requirements should explicitly account for the fact that those bindings are not maintained by the DuckDB Foundation itself. citeturn16search3

### For UWP

The least-risk path is **managed `Apache.Arrow` using its .NET Standard 2.0 asset**, with UWP Minimum Version 16299 or later, followed by explicit package/API testing. Microsoft's framework compatibility makes that route plausible; Apache itself does not advertise UWP as a tested Arrow platform. citeturn23search6turn23search8turn18search0

I would **not** select full Arrow C++ or DuckDB for a new UWP architecture without a proof-of-concept build. DuckDB's vcpkg port explicitly excludes UWP, while Arrow's native Windows documentation is aimed at normal Visual Studio/MSVC Windows builds rather than UWP. citeturn16search2turn20search0

### Overall recommendation

For Microsoft's mainstream application models, there is **no need to seek a third-party Windows port of Arrow**:

**C#/.NET → `Apache.Arrow` NuGet.**  
**Win32/C++ → upstream Arrow C++ through vcpkg + CMake/MSVC.**  
**Tiny native interchange component → Apache nanoarrow.**  
**Local SQL/vectorized analytics → Arrow + DuckDB.**

The only major reason to reject upstream Arrow on the grounds that it is "not Windows-native" would be based on a mistaken premise. Both the C++ and C# implementations execute natively in their respective Microsoft runtime/toolchain environments and require no Python. The legitimate reservations are different: **Arrow is Apache-governed rather than Microsoft-governed; C++ remains CMake-centric rather than MSBuild-native; UWP is not a dedicated target; there is no Arrow MSIX SDK; and most performance optimization is architecture/cross-platform optimization rather than explicit Windows-kernel specialization.** citeturn20search0turn18search0turn23search0turn21search0turn21search2

The official starting points are [Apache Arrow installation](https://arrow.apache.org/install/), [Arrow C++ Windows development](https://arrow.apache.org/docs/developers/cpp/windows.html), [Arrow C++ CMake integration](https://arrow.apache.org/docs/cpp/build_system.html), [Apache Arrow .NET](https://arrow.apache.org/dotnet/), [Apache Arrow .NET repository](https://github.com/apache/arrow-dotnet), [Apache.Arrow on NuGet](https://www.nuget.org/packages/Apache.Arrow/), [Arrow on vcpkg](https://vcpkg.io/en/package/arrow.html), [Apache nanoarrow](https://arrow.apache.org/nanoarrow/), [Microsoft.Data.Analysis](https://www.nuget.org/packages/Microsoft.Data.Analysis/), and [DuckDB](https://duckdb.org/). Current release/package data in this report was checked against those primary sources on August 27, 2026. citeturn23search0turn23search1turn18search1turn17search3turn15search1turn22search9