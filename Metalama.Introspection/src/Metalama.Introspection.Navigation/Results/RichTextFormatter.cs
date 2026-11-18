// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Metalama.Introspection.Navigation.Results;

/// <summary>
/// Formats the arguments of a <see cref="FormattableString"/> with <see cref="MetalamaStringFormatter"/> and replaces the
/// arguments that are objects by placeholders, so that they can become nodes.
/// </summary>
internal sealed class RichTextFormatter : ICustomFormatter, IFormatProvider
{
    public const string Separator = "\u0001";
    public const string ArgumentPrefix = "\u0002";

    public List<(string Text, object Value)> Arguments { get; } = [];

    public string Format( string? format, object? arg, IFormatProvider? formatProvider )
    {
        var text = FormatArgument( format, arg );

        if ( arg == null || arg is string || arg.GetType().IsPrimitive || arg is Enum )
        {
            return text;
        }

        var index = this.Arguments.Count;
        this.Arguments.Add( (text, arg) );

        return $"{Separator}{ArgumentPrefix}{index.ToString( CultureInfo.InvariantCulture )}{Separator}";
    }

    public object? GetFormat( Type? formatType ) => formatType == typeof(ICustomFormatter) ? this : null;

    private static string FormatArgument( string? format, object? arg )
    {
        try
        {
            return MetalamaStringFormatter.Instance.Format( format, arg, CultureInfo.InvariantCulture );
        }
        catch ( InvalidOperationException )
        {
            // The formatter is initialized with the Metalama engine, which may not have run yet.
            return arg is IFormattable formattable ? formattable.ToString( format, CultureInfo.InvariantCulture ) : arg?.ToString() ?? "";
        }
    }
}
