// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Utilities;

internal static class TypeNameHelper
{
    /// <summary>
    /// Removes the generic arity suffix (<c>`n</c>) from each part of a name, e.g. <c>Ns.Foo`2.Bar`1</c> to
    /// <c>Ns.Foo.Bar</c>. Only valid where a type argument list supplies the arity instead.
    /// </summary>
    public static string StripArity( string name )
    {
        while ( true )
        {
            var backtick = name.IndexOfOrdinal( '`' );

            if ( backtick < 0 )
            {
                return name;
            }

            var end = backtick + 1;

            while ( end < name.Length && char.IsDigit( name[end] ) )
            {
                end++;
            }

            name = name.Remove( backtick, end - backtick );
        }
    }

    public static (string? Namespace, string Name) SplitNamespaceAndName( string reflectionFullName )
    {
        var firstNestedTypeSeparator = reflectionFullName.IndexOfOrdinal( '+' );
        var namespaceScope = firstNestedTypeSeparator >= 0 ? reflectionFullName[..firstNestedTypeSeparator] : reflectionFullName;
        var namespaceEnd = namespaceScope.LastIndexOf( '.' );
        var lastSeparator = reflectionFullName.LastIndexOfAny( ['.', '+'] );

        var ns = namespaceEnd >= 0 ? namespaceScope[..namespaceEnd] : null;
        var name = lastSeparator >= 0 ? reflectionFullName[(lastSeparator + 1)..] : reflectionFullName;

        return (ns, name);
    }
}