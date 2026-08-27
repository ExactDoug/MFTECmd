using System;
using MFT.Attributes;

namespace MFTECmd;

public class MFTRecordOut : IArrowRecord
{
    public uint EntryNumber { get; set; }
    public ushort SequenceNumber { get; set; }

    public uint ParentEntryNumber { get; set; }
    public ushort? ParentSequenceNumber { get; set; }

    public bool InUse { get; set; }
    public string ParentPath { get; set; }
    public string FileName { get; set; }

    public string Extension { get; set; }

    public bool IsDirectory { get; set; }
    public bool HasAds { get; set; }
    public bool IsAds { get; set; }

    public ulong FileSize { get; set; }

    public DateTimeOffset? Created0x10 { get; set; }
    public DateTimeOffset? Created0x30 { get; set; }

    public DateTimeOffset? LastModified0x10 { get; set; }
    public DateTimeOffset? LastModified0x30 { get; set; }

    public DateTimeOffset? LastRecordChange0x10 { get; set; }
    public DateTimeOffset? LastRecordChange0x30 { get; set; }

    public DateTimeOffset? LastAccess0x10 { get; set; }

    public DateTimeOffset? LastAccess0x30 { get; set; }

    public long UpdateSequenceNumber { get; set; }
    public long LogfileSequenceNumber { get; set; }

    public int SecurityId { get; set; }

    public string ZoneIdContents { get; set; }
    public StandardInfo.Flag SiFlags { get; set; }
    public string ObjectIdFileDroid { get; set; }
    public string ReparseTarget { get; set; }
    public int ReferenceCount { get; set; }
    public NameTypes NameType { get; set; }
    public string LoggedUtilStream { get; set; }
    public bool Timestomped { get; set; }
    public bool uSecZeros { get; set; }
    public bool Copied { get; set; }

    public int FnAttributeId { get; set; }
    public int OtherAttributeId { get; set; }
    public string SourceFile { get; set; }
}

public class FileListEntry
{
    /// <summary>
    /// Constructor for --fl mode (creates from MFTRecordOut)
    /// </summary>
    public FileListEntry(MFTRecordOut r)
    {
        FullPath = $"{r.ParentPath}\\{r.FileName}";
        Extension = r.Extension;
        IsDirectory = r.IsDirectory;
        FileSize = r.FileSize;
        Created0x10 = r.Created0x10;
        LastModified0x10 = r.LastModified0x10;
    }

    /// <summary>
    /// Constructor for --flo mode, from values already gathered into a <see cref="FileListData" />.
    /// </summary>
    public FileListEntry(FileListData data)
    {
        FullPath = $"{data.ParentPath}\\{data.FileName}";
        Extension = data.Extension;
        IsDirectory = data.IsDirectory;
        FileSize = data.FileSize;
        Created0x10 = data.Created0x10;
        LastModified0x10 = data.LastModified0x10;
    }

    /// <summary>
    /// Direct constructor for --flo mode (bypasses MFTRecordOut for efficiency)
    /// </summary>
    public FileListEntry(string parentPath, string fileName, string extension,
                         bool isDirectory, ulong fileSize,
                         DateTimeOffset? created0x10, DateTimeOffset? lastModified0x10)
    {
        FullPath = $"{parentPath}\\{fileName}";
        Extension = extension;
        IsDirectory = isDirectory;
        FileSize = fileSize;
        Created0x10 = created0x10;
        LastModified0x10 = lastModified0x10;
    }

    public string FullPath { get; set; }
    public string Extension { get; set; }

    public bool IsDirectory { get; set; }
    public ulong FileSize { get; set; }
    public DateTimeOffset? Created0x10 { get; set; }
    public DateTimeOffset? LastModified0x10 { get; set; }
}