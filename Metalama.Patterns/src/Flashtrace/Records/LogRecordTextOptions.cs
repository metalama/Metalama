// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Flashtrace.Records;

/// <summary>
/// Options of the <see cref="ILogRecordBuilder.BeginWriteItem"/> method.
/// </summary>
[PublicAPI]
public readonly struct LogRecordTextOptions
{
    /// <summary>
    /// Gets the semantic name of the message, or <c>null</c> for a non-semantic message.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the number of parameters in the message.
    /// </summary>
    public int ParameterCount { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogRecordTextOptions"/> struct.
    /// </summary>
    /// <param name="parameterCount">Number of parameters in the message.</param>
    /// <param name="name">Semantic name of the message, or <c>null</c> for a non-semantic message.</param>
    public LogRecordTextOptions( int parameterCount, string? name = null )
    {
        this.Name = name;
        this.ParameterCount = parameterCount;
    }
}