// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Text;

namespace Metalama.Patterns.Wpf.Implementation;

// Prevent netframework-only false positives
// ReSharper disable once RedundantBlankLines

#if NETFRAMEWORK

// ReSharper disable AssignNullToNotNullAttribute
#endif

[CompileTime]
internal static class FormattingExtensions
{
    public static string PrettyList( this IEnumerable<string> words, string conjunction, char quote = default )
        => PrettyList( words, conjunction, out _, quote );

    // ReSharper disable once OutParameterValueIsAlwaysDiscarded.Global
    public static string PrettyList( this IEnumerable<string> words, string conjunction, out int plurality, char quote = default )
    {
        using var iter = words.GetEnumerator();

        var a = iter.MoveNext() ? iter.Current : null;

        if ( a == null )
        {
            plurality = 0;

            return string.Empty;
        }

        var b = iter.MoveNext() ? iter.Current : null;

        if ( b == null )
        {
            plurality = 1;

            return quote == default
                ? a
                : new StringBuilder().AppendQuoted( a, quote ).ToString();
        }

        plurality = 2;
        var sb = new StringBuilder();

        while ( iter.MoveNext() )
        {
            sb.AppendQuoted( a, quote ).Append( ',' ).Append( ' ' );

            a = b;
            b = iter.Current;
        }

        sb.AppendQuoted( a, quote ).Append( conjunction ).Append( b );

        return sb.ToString();
    }

    private static StringBuilder AppendQuoted( this StringBuilder sb, string s, char quote )
    {
        if ( quote == default )
        {
            return sb.Append( s );
        }
        else
        {
            return sb.Append( quote ).Append( s ).Append( quote );
        }
    }
}