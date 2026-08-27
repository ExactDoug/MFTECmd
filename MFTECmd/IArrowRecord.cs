using System;
using MFT.Attributes;

namespace MFTECmd;

/// <summary>
/// The set of fields <see cref="MftArrowWriter" /> needs in order to emit a record.
/// </summary>
/// <remarks>
/// Implemented by both <see cref="MFTRecordOut" /> (the full processing path) and
/// <see cref="FloArrowRecord" /> (the --flo fast path). Keeping the writer bound to this
/// interface rather than to <see cref="MFTRecordOut" /> lets --flo emit Arrow output without
/// allocating the intermediate <see cref="MFTRecordOut" /> it exists to avoid, while still
/// producing an identical schema in both modes.
/// </remarks>
public interface IArrowRecord
{
    uint EntryNumber { get; }
    uint ParentEntryNumber { get; }
    string ParentPath { get; }
    string FileName { get; }
    string Extension { get; }
    ulong FileSize { get; }
    bool IsDirectory { get; }
    DateTimeOffset? Created0x10 { get; }
    DateTimeOffset? LastModified0x10 { get; }
    NameTypes NameType { get; }
    bool InUse { get; }
}
