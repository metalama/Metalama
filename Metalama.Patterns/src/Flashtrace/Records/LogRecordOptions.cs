// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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