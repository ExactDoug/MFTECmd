using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Apache.Arrow;
using Apache.Arrow.Ipc;
using Apache.Arrow.Types;
using MFT.Attributes;

namespace MFTECmd;

/// <summary>
/// Writes MFT record data to Apache Arrow IPC format with streaming batch writes for memory efficiency.
/// </summary>
/// <remarks>
/// This class buffers records and writes them in batches to minimize memory usage when processing
/// large MFT files. The default batch size is 10,000 records.
/// </remarks>
public sealed class MftArrowWriter : IDisposable
{
    /// <summary>
    /// Default number of records per batch for streaming writes.
    /// </summary>
    public const int DefaultBatchSize = 10_000;

    /// <summary>
    /// Timezone identifier used for timestamp fields.
    /// </summary>
    private const string TimestampTimezone = "UTC";

    private readonly Stream _outputStream;
    private readonly ArrowFileWriter _writer;
    private readonly Schema _schema;
    private readonly int _batchSize;
    private readonly bool _ownsStream;

    // Builders for current batch
    private readonly UInt32Array.Builder _entryNumberBuilder;
    private readonly UInt32Array.Builder _parentEntryNumberBuilder;
    private readonly StringArray.Builder _parentPathBuilder;
    private readonly StringArray.Builder _fileNameBuilder;
    private readonly StringArray.Builder _extensionBuilder;
    private readonly UInt64Array.Builder _fileSizeBuilder;
    private readonly BooleanArray.Builder _isDirectoryBuilder;
    private readonly TimestampArray.Builder _created0x10Builder;
    private readonly TimestampArray.Builder _lastModified0x10Builder;
    private readonly StringArray.Builder _nameTypeBuilder;
    private readonly BooleanArray.Builder _inUseBuilder;

    private int _currentBatchCount;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MftArrowWriter"/> class.
    /// </summary>
    /// <param name="outputPath">The file path to write the Arrow IPC file.</param>
    /// <param name="batchSize">Number of records per batch. Defaults to 10,000.</param>
    /// <exception cref="ArgumentNullException">Thrown when outputPath is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when batchSize is less than 1.</exception>
    public MftArrowWriter(string outputPath, int batchSize = DefaultBatchSize)
        : this(new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous), batchSize, ownsStream: true)
    {
        if (string.IsNullOrEmpty(outputPath))
            throw new ArgumentNullException(nameof(outputPath));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MftArrowWriter"/> class using an existing stream.
    /// </summary>
    /// <param name="outputStream">The stream to write the Arrow IPC data.</param>
    /// <param name="batchSize">Number of records per batch. Defaults to 10,000.</param>
    /// <param name="ownsStream">Whether the writer should dispose the stream when disposed.</param>
    /// <exception cref="ArgumentNullException">Thrown when outputStream is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when batchSize is less than 1.</exception>
    public MftArrowWriter(Stream outputStream, int batchSize = DefaultBatchSize, bool ownsStream = false)
    {
        _outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));

        if (batchSize < 1)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be at least 1.");

        _batchSize = batchSize;
        _ownsStream = ownsStream;
        _schema = CreateSchema();

        // Initialize the Arrow file writer
        _writer = new ArrowFileWriter(_outputStream, _schema);

        // Initialize array builders with capacity hint
        _entryNumberBuilder = new UInt32Array.Builder().Reserve(batchSize);
        _parentEntryNumberBuilder = new UInt32Array.Builder().Reserve(batchSize);
        _parentPathBuilder = new StringArray.Builder();
        _fileNameBuilder = new StringArray.Builder();
        _extensionBuilder = new StringArray.Builder();
        _fileSizeBuilder = new UInt64Array.Builder().Reserve(batchSize);
        _isDirectoryBuilder = new BooleanArray.Builder().Reserve(batchSize);
        _created0x10Builder = new TimestampArray.Builder(new TimestampType(TimeUnit.Microsecond, TimestampTimezone)).Reserve(batchSize);
        _lastModified0x10Builder = new TimestampArray.Builder(new TimestampType(TimeUnit.Microsecond, TimestampTimezone)).Reserve(batchSize);
        _nameTypeBuilder = new StringArray.Builder();
        _inUseBuilder = new BooleanArray.Builder().Reserve(batchSize);

        _currentBatchCount = 0;
    }

    /// <summary>
    /// Gets the number of records currently buffered in the current batch.
    /// </summary>
    public int CurrentBatchCount => _currentBatchCount;

    /// <summary>
    /// Gets the configured batch size.
    /// </summary>
    public int BatchSize => _batchSize;

    /// <summary>
    /// Creates the Arrow schema for MFT records.
    /// </summary>
    private static Schema CreateSchema()
    {
        var timestampType = new TimestampType(TimeUnit.Microsecond, TimestampTimezone);

        var fields = new List<Field>
        {
            new Field("EntryNumber", UInt32Type.Default, nullable: false),
            new Field("ParentEntryNumber", UInt32Type.Default, nullable: false),
            new Field("ParentPath", StringType.Default, nullable: true),
            new Field("FileName", StringType.Default, nullable: false),
            new Field("Extension", StringType.Default, nullable: true),
            new Field("FileSize", UInt64Type.Default, nullable: false),
            new Field("IsDirectory", BooleanType.Default, nullable: false),
            new Field("Created0x10", timestampType, nullable: true),
            new Field("LastModified0x10", timestampType, nullable: true),
            new Field("NameType", StringType.Default, nullable: false),
            new Field("InUse", BooleanType.Default, nullable: false)
        };

        return new Schema(fields, null);
    }

    /// <summary>
    /// Writes a single MFT record to the buffer asynchronously.
    /// When the batch size is reached, the batch is automatically flushed to disk.
    /// </summary>
    /// <param name="record">The MFT record to write.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when record is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public async Task WriteRecordAsync(IArrowRecord record)
    {
        ThrowIfDisposed();

        if (record == null)
            throw new ArgumentNullException(nameof(record));

        // Append values to builders
        _entryNumberBuilder.Append(record.EntryNumber);
        _parentEntryNumberBuilder.Append(record.ParentEntryNumber);

        if (record.ParentPath != null)
            _parentPathBuilder.Append(record.ParentPath);
        else
            _parentPathBuilder.AppendNull();

        _fileNameBuilder.Append(record.FileName ?? string.Empty);

        if (record.Extension != null)
            _extensionBuilder.Append(record.Extension);
        else
            _extensionBuilder.AppendNull();

        _fileSizeBuilder.Append(record.FileSize);
        _isDirectoryBuilder.Append(record.IsDirectory);

        AppendTimestamp(_created0x10Builder, record.Created0x10);
        AppendTimestamp(_lastModified0x10Builder, record.LastModified0x10);

        _nameTypeBuilder.Append(record.NameType.ToString());
        _inUseBuilder.Append(record.InUse);

        _currentBatchCount++;

        // Flush batch if we've reached the batch size
        if (_currentBatchCount >= _batchSize)
        {
            await FlushBatchAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Writes a single MFT record to the buffer synchronously.
    /// When the batch size is reached, the batch is automatically flushed to disk.
    /// </summary>
    /// <param name="record">The MFT record to write.</param>
    /// <exception cref="ArgumentNullException">Thrown when record is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public void WriteRecord(IArrowRecord record)
    {
        WriteRecordAsync(record).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Appends a nullable DateTimeOffset to a timestamp builder.
    /// </summary>
    private static void AppendTimestamp(TimestampArray.Builder builder, DateTimeOffset? value)
    {
        if (value.HasValue)
        {
            builder.Append(value.Value);
        }
        else
        {
            builder.AppendNull();
        }
    }

    /// <summary>
    /// Flushes the current batch to the output stream asynchronously.
    /// This method is called automatically when the batch size is reached.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public async Task FlushAsync()
    {
        ThrowIfDisposed();

        if (_currentBatchCount > 0)
        {
            await FlushBatchAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Flushes the current batch to the output stream synchronously.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public void Flush()
    {
        FlushAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Writes the current batch to the Arrow file and resets the builders.
    /// </summary>
    private async Task FlushBatchAsync()
    {
        if (_currentBatchCount == 0)
            return;

        // Build arrays from builders
        var arrays = new IArrowArray[]
        {
            _entryNumberBuilder.Build(),
            _parentEntryNumberBuilder.Build(),
            _parentPathBuilder.Build(),
            _fileNameBuilder.Build(),
            _extensionBuilder.Build(),
            _fileSizeBuilder.Build(),
            _isDirectoryBuilder.Build(),
            _created0x10Builder.Build(),
            _lastModified0x10Builder.Build(),
            _nameTypeBuilder.Build(),
            _inUseBuilder.Build()
        };

        // Create and write the record batch
        var recordBatch = new RecordBatch(_schema, arrays, _currentBatchCount);
        await _writer.WriteRecordBatchAsync(recordBatch).ConfigureAwait(false);

        // Clear builders for next batch by re-initializing them
        ClearBuilders();
        _currentBatchCount = 0;
    }

    /// <summary>
    /// Clears the array builders for the next batch.
    /// </summary>
    private void ClearBuilders()
    {
        _entryNumberBuilder.Clear();
        _parentEntryNumberBuilder.Clear();
        _parentPathBuilder.Clear();
        _fileNameBuilder.Clear();
        _extensionBuilder.Clear();
        _fileSizeBuilder.Clear();
        _isDirectoryBuilder.Clear();
        _created0x10Builder.Clear();
        _lastModified0x10Builder.Clear();
        _nameTypeBuilder.Clear();
        _inUseBuilder.Clear();
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the writer has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MftArrowWriter));
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="MftArrowWriter"/>
    /// and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            // Flush any remaining records
            if (_currentBatchCount > 0)
            {
                FlushBatchAsync().GetAwaiter().GetResult();
            }

            // Complete the Arrow file (writes footer)
            _writer.WriteEnd();
            _writer.Dispose();
        }
        finally
        {
            if (_ownsStream)
            {
                _outputStream.Dispose();
            }
        }
    }

    /// <summary>
    /// Asynchronously releases resources used by the <see cref="MftArrowWriter"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            // Flush any remaining records
            if (_currentBatchCount > 0)
            {
                await FlushBatchAsync().ConfigureAwait(false);
            }

            // Complete the Arrow file (writes footer)
            await _writer.WriteEndAsync().ConfigureAwait(false);
            _writer.Dispose();
        }
        finally
        {
            if (_ownsStream)
            {
#if NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
                    await _outputStream.DisposeAsync().ConfigureAwait(false);
#else
                    _outputStream.Dispose();
#endif
            }
        }
    }
}
