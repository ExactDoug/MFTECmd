using System;

namespace MFTECmd;

/// <summary>
/// The values extracted once per record by the --flo fast path.
/// </summary>
/// <remarks>
/// A readonly struct so gathering these costs no heap allocation. Resolving the parent path is
/// the expensive part of --flo extraction, so it is done once here and then shared by every
/// consumer (the file listing CSV and, when --arrow is active, the Arrow record).
/// </remarks>
public readonly struct FileListData
{
    public FileListData(string parentPath, string fileName, string extension,
                        bool isDirectory, ulong fileSize,
                        DateTimeOffset? created0x10, DateTimeOffset? lastModified0x10)
    {
        ParentPath = parentPath;
        FileName = fileName;
        Extension = extension;
        IsDirectory = isDirectory;
        FileSize = fileSize;
        Created0x10 = created0x10;
        LastModified0x10 = lastModified0x10;
    }

    public string ParentPath { get; }
    public string FileName { get; }
    public string Extension { get; }
    public bool IsDirectory { get; }
    public ulong FileSize { get; }
    public DateTimeOffset? Created0x10 { get; }
    public DateTimeOffset? LastModified0x10 { get; }
}
