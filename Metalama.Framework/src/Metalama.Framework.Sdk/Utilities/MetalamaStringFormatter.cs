// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Globalization;

namespace Metalama.Framework.Engine.Utilities;

/// <summary>
/// Formats arguments passed to a diagnostic.
/// </summary>
public abstract class MetalamaStringFormatter : CultureInfo, ICustomFormatter
{
    private static MetalamaStringFormatter? _instance;

    [PublicAPI]
    public static MetalamaStringFormatter Instance => _instance ?? throw new InvalidOperationException( "The class has not been initialized." );

    /// <summary>
    /// Gets the instance, or <c>null</c> when the class has not been initialized, for the callers that format
    /// eagerly and must not turn the absence of an implementation into an exception.
    /// </summary>
    /// <remarks>
    /// <see cref="Instance"/> throws, which is right for formatting a message that is about to be displayed: there is
    /// no message without a formatter. It is wrong for a caller that formats ahead of time as an optimization, because
    /// such a caller has a correct fallback, namely to do nothing and let the value be formatted later.
    /// </remarks>
    internal static MetalamaStringFormatter? InstanceOrNull => _instance;

    internal static void Initialize( MetalamaStringFormatter impl ) => _instance = impl;

    private protected MetalamaStringFormatter() : base( InvariantCulture.Name ) { }

    public override object? GetFormat( Type? formatType ) => formatType == typeof(ICustomFormatter) ? this : base.GetFormat( formatType );

    public static string Format( FormattableString message ) => message.ToString( Instance );

    public abstract string Format( string? format, object? arg, IFormatProvider? formatProvider );

    // ReSharper disable once MemberCanBeInternal
    public string Format( object? arg ) => this.Format( null, arg, null );
}