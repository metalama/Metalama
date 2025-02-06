// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Flashtrace;

/// <summary>
/// Specifies the severity of a logged message.
/// </summary>
[PublicAPI]
public enum FlashtraceLevel
{
    /// <summary>
    /// No message should be logged.
    /// </summary>
    /// <remarks>The value is <c>0</c>.</remarks>
    None = 0,

    /// <summary>
    /// The message should be logged at Trace level (when applicable).
    /// </summary>
    Trace = 1,

    /// <summary>
    /// The message should be logged at Debug level (when applicable).
    /// </summary>
    Debug = 2,

    /// <summary>
    /// The message should be logged at Info level (when applicable).
    /// </summary>
    Info = 3,

    /// <summary>
    /// The message should be logged at Warning level (when applicable).
    /// </summary>
    Warning = 4,

    /// <summary>
    /// The message should be logged at Error level (when applicable).
    /// </summary>
    Error = 5,

    /// <summary>
    /// The message should be logged at Critical level (when applicable).
    /// </summary>
    Critical = 6
}