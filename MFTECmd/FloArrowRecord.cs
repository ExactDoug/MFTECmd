using System;
using MFT;
using MFT.Attributes;
using MFT.Other;

namespace MFTECmd;

/// <summary>
/// <see cref="IArrowRecord" /> implementation for the --flo fast path.
/// </summary>
/// <remarks>
/// Every field here comes either from <see cref="FileListData" /> (already gathered for the file
/// listing CSV) or from the record header and the FileName attribute, both of which --flo has
/// already parsed. No attribute outside the --flo filter set (StandardInformation, FileName, Data)
/// is touched, so emitting Arrow from --flo costs no additional parsing.
/// Instances are created only when --arrow is active.
/// </remarks>
public sealed class FloArrowRecord : IArrowRecord
{
    public FloArrowRecord(FileListData data, FileRecord fr, FileName fn)
    {
        ParentPath = data.ParentPath;
        FileName = data.FileName;
        Extension = data.Extension;
        IsDirectory = data.IsDirectory;
        FileSize = data.FileSize;
        Created0x10 = data.Created0x10;
        LastModified0x10 = data.LastModified0x10;

        EntryNumber = fr.EntryNumber;
        InUse = fr.IsDeleted() == false;
        ParentEntryNumber = fn.FileInfo.ParentMftRecord.MftEntryNumber;
        NameType = fn.FileInfo.NameType;
    }

    public uint EntryNumber { get; }
    public uint ParentEntryNumber { get; }
    public string ParentPath { get; }
    public string FileName { get; }
    public string Extension { get; }
    public ulong FileSize { get; }
    public bool IsDirectory { get; }
    public DateTimeOffset? Created0x10 { get; }
    public DateTimeOffset? LastModified0x10 { get; }
    public NameTypes NameType { get; }
    public bool InUse { get; }
}
