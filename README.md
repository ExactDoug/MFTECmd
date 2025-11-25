# MFTECmd

## Command Line Interface

    MFTECmd version 0.5.0.1
    
    Author: Eric Zimmerman (saericzimmerman@gmail.com)
    https://github.com/EricZimmerman/MFTECmd
    
            f               File to process ($MFT | $J | $LogFile | $Boot | $SDS). Required
    
            json            Directory to save JSON formatted results to. This or --csv required unless --de or --body is specified
            jsonf           File name to save JSON formatted results to. When present, overrides default name
            csv             Directory to save CSV formatted results to. This or --json required unless --de or --body is specified
            csvf            File name to save CSV formatted results to. When present, overrides default name
    
            body            Directory to save bodyfile formatted results to. --bdl is also required when using this option
            bodyf           File name to save body formatted results to. When present, overrides default name
            bdl             Drive letter (C, D, etc.) to use with bodyfile. Only the drive letter itself should be provided
            blf             When true, use LF vs CRLF for newlines. Default is FALSE
    
            dd              Directory to save exported FILE record. --do is also required when using this option
            do              Offset of the FILE record to dump as decimal or hex. Ex: 5120 or 0x1400 Use --de or --vl 1 to see offsets
    
            de              Dump full details for entry/sequence #. Format is 'Entry' or 'Entry-Seq' as decimal or hex. Example: 5, 624-5 or 0x270-0x5.
            fls             When true, displays contents of directory specified by --de. Ignored when --de points to a file.
            ds              Dump full details for Security Id as decimal or hex. Example: 624 or 0x270
    
            dt              The custom date/time format to use when displaying time stamps. Default is: yyyy-MM-dd HH:mm:ss.fffffff
            sn              Include DOS file name types. Default is FALSE
            fl              Generate condensed file listing. Requires --csv. Default is FALSE
            flo             Generate file listing ONLY (no full CSV). Optimized for ~38% faster processing. Requires --csv
            at              When true, include all timestamps from 0x30 attribute vs only when they differ from 0x10. Default is FALSE
    
            vss             Process all Volume Shadow Copies that exist on drive specified by -f . Default is FALSE
            dedupe          Deduplicate -f & VSCs based on SHA-1. First file found wins. Default is FALSE
    
            debug           Show debug information during processing
            trace           Show trace information during processing


    Examples: MFTECmd.exe -f "C:\Temp\SomeMFT" --csv "c:\temp\out" --csvf MyOutputFile.csv
              MFTECmd.exe -f "C:\Temp\SomeMFT" --csv "c:\temp\out"
              MFTECmd.exe -f "C:\Temp\SomeMFT" --json "c:\temp\jsonout"
              MFTECmd.exe -f "C:\Temp\SomeMFT" --body "c:\temp\bout" --bdl c
              MFTECmd.exe -f "C:\Temp\SomeMFT" --de 5-5
              MFTECmd.exe -f "C:\Temp\SomeMFT" --csv "c:\temp\out" --flo

              Short options (single letter) are prefixed with a single dash. Long commands are prefixed with two dashes

### File Listing Only Mode (--flo)

The `--flo` option generates only the condensed file listing CSV without the full MFT record export. This mode is optimized for scenarios where you only need basic file metadata (path, extension, size, timestamps) and provides approximately 38% faster processing compared to the standard `--fl` option.

Output columns: FullPath, Extension, IsDirectory, FileSize, Created0x10, LastModified0x10

## Performance Optimizations

This fork includes optimizations that reduce processing time by approximately 38% compared to the standard `--fl` option. The optimizations span both MFTECmd and the MFT library.

### How It Works

When processing an NTFS $MFT file, the standard approach parses all attributes for every file record, then extracts the full set of metadata fields. For use cases that only need basic file listing data, this is wasteful.

The `--flo` mode implements two key optimizations:

#### 1. MFTECmd: Streamlined Output Path

**File:** `MFTECmd/Program.cs`

- **New `--flo` command-line option**: When specified, skips generation of the full CSV export entirely
- **Optimized `GetFileListData()` function**: Extracts only the 6 required fields (FullPath, Extension, IsDirectory, FileSize, Created0x10, LastModified0x10) directly from file records
- **Direct `FileListEntry` constructor**: Bypasses intermediate `MFTRecordOut` object creation, reducing memory allocations

#### 2. MFT Library: Selective Attribute Parsing

**Files:** `mft/MFT/MftFile.cs`, `mft/MFT/Mft.cs`, `mft/MFT/FileRecord.cs`

- **Attribute filter parameter**: New constructor overloads accept a `HashSet<AttributeType>` specifying which attributes to parse
- **Early-exit optimization**: When parsing file records, attributes not in the filter set are skipped entirely (just advance the index pointer)
- **Filtered attributes for --flo mode**:
  - `StandardInformation` (0x10): Created/Modified timestamps
  - `FileName` (0x30): File name and parent directory reference
  - `Data` (0x80): File size

Attributes skipped in `--flo` mode include: AttributeList, ObjectId, SecurityDescriptor, VolumeName, VolumeInformation, IndexRoot, IndexAllocation, Bitmap, ReparsePoint, EaInformation, Ea, LoggedUtilityStream.

### Performance Impact

| Mode | Processing Time | Improvement |
|------|-----------------|-------------|
| Standard `--fl` | ~133 seconds | baseline |
| `--flo` (Debug build) | ~87 seconds | 35% faster |
| `--flo` (Release build) | ~80 seconds | 40% faster |

*Benchmark: 2.1GB $MFT with ~2.15 million file records*

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         MFTECmd                                 │
│  ┌─────────────────┐    ┌─────────────────────────────────────┐ │
│  │ --flo option    │───>│ GetFileListData()                   │ │
│  │                 │    │ - Extracts only 6 fields            │ │
│  └─────────────────┘    │ - Direct FileListEntry construction │ │
│           │             └─────────────────────────────────────┘ │
│           │                              │                      │
│           v                              v                      │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ Attribute Filter: {StandardInfo, FileName, Data}            ││
│  └─────────────────────────────────────────────────────────────┘│
└───────────────────────────────│─────────────────────────────────┘
                                │
                                v
┌─────────────────────────────────────────────────────────────────┐
│                      MFT Library (submodule)                    │
│  ┌─────────────────┐    ┌─────────────────────────────────────┐ │
│  │ MftFile.Load()  │───>│ FileRecord constructor              │ │
│  │ + filter param  │    │ - Skips non-filtered attributes     │ │
│  └─────────────────┘    │ - Reduces parsing overhead          │ │
│                         └─────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Documentation

MFT parser for NTFS file systems.

[Introducing MFTECmd!](https://binaryforay.blogspot.com/2018/06/introducing-mftecmd.html)

[MFTECmd v0.2.6.0 released](https://binaryforay.blogspot.com/2018/06/mftecmd-v0260-released.html)

[MFTECmd 0.3.6.0 released](https://binaryforay.blogspot.com/2018/12/mftecmd-0360-released.html)

[Locked file support added to AmcacheParser, AppCompatCacheParser, MFTECmd, ShellBags Explorer (and SBECmd), and Registry Explorer (and RECmd)](https://binaryforay.blogspot.com/2019/01/locked-file-support-added-to.html)

## Building from Source

### Prerequisites

- [.NET SDK 9.0](https://dotnet.microsoft.com/download) or later
- Git

### Clone the Repository

This repository uses a git submodule for the MFT library with optimized attribute filtering. Clone with submodules:

```bash
git clone --recurse-submodules https://github.com/ExactDoug/MFTECmd.git
cd MFTECmd
```

If you already cloned without submodules:

```bash
git submodule update --init --recursive
```

### Checkout the Optimized MFT Branch

The MFT submodule should be on the branch with attribute filtering optimizations:

```bash
cd mft
git checkout claude/explore-mft-parsing-01NHZnRVbm7yN5mF5jge5NjQ
cd ..
```

### Build

Build the solution in Release mode:

```bash
dotnet build MFTECmd.sln -c Release
```

The executable will be at:
- Windows: `MFTECmd/bin/Release/net9.0/MFTECmd.exe`
- Cross-platform: `MFTECmd/bin/Release/net9.0/MFTECmd.dll` (run with `dotnet MFTECmd.dll`)

### Reverting to NuGet MFT Package

To use the standard NuGet MFT package instead of the submodule (without `--flo` optimizations):

1. Edit `MFTECmd/MFTECmd.csproj`
2. Replace the `<ProjectReference>` with: `<PackageReference Include="MFT" Version="1.5.1" />`
3. Remove the MFT project from `MFTECmd.sln`

## Troubleshooting

### Build Issues

**"MFT project not found" or path errors**

Ensure the submodule is initialized:
```bash
git submodule update --init --recursive
```

**MFT builds to Debug when MFTECmd builds Release**

Build using the solution file (not the project file directly):
```bash
dotnet build MFTECmd.sln -c Release
```

The solution file contains configuration mappings that ensure both projects build with the same configuration.

**Missing MFT attribute filtering optimizations**

Verify the MFT submodule is on the correct branch:
```bash
cd mft
git branch
# Should show: claude/explore-mft-parsing-01NHZnRVbm7yN5mF5jge5NjQ
```

### Runtime Issues

**"The target framework 'net6.0' is out of support" warning**

This is a warning only and does not affect the build. The net6.0 target is kept for compatibility. Use the net9.0 build for best performance.

**Permission denied accessing $MFT**

Run as Administrator. The live $MFT file requires elevated privileges. Alternatively, use a copied/extracted $MFT file.

# Download Eric Zimmerman's Tools

All of Eric Zimmerman's tools can be downloaded [here](https://ericzimmerman.github.io/#!index.md). 

# Special Thanks

Open Source Development funding and support provided by the following contributors: 
- [SANS Institute](http://sans.org/) and [SANS DFIR](http://dfir.sans.org/).
- [Tines](https://www.tines.com/?utm_source=oss&utm_medium=sponsorship&utm_campaign=ericzimmerman)
