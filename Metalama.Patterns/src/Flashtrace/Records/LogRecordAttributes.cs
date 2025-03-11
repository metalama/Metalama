// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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