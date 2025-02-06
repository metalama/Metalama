// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Flashtrace.Records;

/// <summary>
/// Attributes of the <see cref="LogParameterOptions"/> class. Describes how the <see cref="ILogRecordBuilder"/> will be used.
/// </summary>
[PublicAPI]
[Flags]
public enum LogRecordAttributes
{
    /// <summary>
    /// Legacy value set by <see cref="IFlashtraceLogger"/> implementations. No information is provided by the caller.
    /// </summary>
    None = 0,

    /// <summary>
    /// The <see cref="ILogRecordBuilder"/> will be used to write the context description.
    /// </summary>
    WriteActivityDescription = 1,

    /// <summary>
    /// The <see cref="ILogRecordBuilder"/> will be used to write the context outcome.
    /// </summary>
    WriteActivityOutcome = 2,

    /// <summary>
    /// The <see cref="ILogRecordBuilder"/> will be used to write the context description and outcome.
    /// </summary>
    WriteActivityDescriptionAndOutcome = WriteActivityDescription | WriteActivityOutcome,

    /// <summary>
    /// The <see cref="ILogRecordBuilder"/> will be used to write a standalone message.
    /// </summary>
    WriteMessage = 4
}