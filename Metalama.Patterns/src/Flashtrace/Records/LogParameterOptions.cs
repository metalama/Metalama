// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Formatters;
using JetBrains.Annotations;

namespace Flashtrace.Records;

/// <summary>
/// Options of the <see cref="ILogRecordBuilder.WriteParameter{T}"/> method.
/// </summary>
[PublicAPI]
public readonly struct LogParameterOptions
{
    // Was [ExplicitCrossPackageInternal]
    public static readonly LogParameterOptions FormattedStringParameter = new( LogParameterMode.Default );

    // Was internal.
    public static readonly LogParameterOptions SemanticParameter = new( LogParameterMode.NameValuePair );

    /// <summary>
    /// Gets rendering mode of the parameter.
    /// </summary>
    public LogParameterMode Mode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogParameterOptions"/> struct.
    /// </summary>
    /// <param name="mode">Determines how the parameter should be rendered.</param>
    public LogParameterOptions( LogParameterMode mode )
    {
        this.Mode = mode;
    }

    // [Pre-FT] This property could be public and settable in the future.
    // Was [ExplicitCrossPackageInternal]
    public FormattingOptions FormattingOptions => this.Mode == LogParameterMode.Default ? FormattingOptions.Unquoted : FormattingOptions.Default;
}