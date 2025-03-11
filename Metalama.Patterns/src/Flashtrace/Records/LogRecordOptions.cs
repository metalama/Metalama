// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Flashtrace.Records;

/// <summary>
/// Options of the <see cref="IFlashtraceLocalLogger.GetRecordBuilder"/> method.
/// 
/// </summary>
[PublicAPI]
public readonly struct LogRecordOptions
{
    /// <summary>
    /// Gets the severity of the log record.
    /// </summary>
    public FlashtraceLevel Level { get; }

    /// <summary>
    /// Gets the kind of log the record (typically <see cref="LogRecordKind.ActivityEntry"/>, <see cref="LogRecordKind.ActivityExit"/>
    /// or <see cref="LogRecordKind.Message"/>).
    /// </summary>
    public LogRecordKind Kind { get; }

    /// <summary>
    /// Gets flags indicating how the log record will be used.
    /// </summary>
    public LogRecordAttributes Attributes { get; }

    /// <summary>
    /// Gets the <see cref="LogEventData"/> for the current record.
    /// </summary>
    public LogEventData Data { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogRecordOptions"/> struct.
    /// Initializes a new <see cref="LogRecordOptions"/>.
    /// </summary>
    /// <param name="level"></param>
    /// <param name="kind"></param>
    /// <param name="attributes"></param>
    /// <param name="data"></param>
    internal LogRecordOptions( FlashtraceLevel level, LogRecordKind kind, LogRecordAttributes attributes, in LogEventData data )
    {
        this.Level = level;
        this.Kind = kind;
        this.Attributes = attributes;
        this.Data = data;
    }
}