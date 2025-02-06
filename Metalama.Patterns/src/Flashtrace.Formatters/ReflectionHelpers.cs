// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Runtime.CompilerServices;

namespace Flashtrace.Formatters;

internal static class ReflectionHelpers
{
    public static bool IsAnonymous( this Type type )
        => type.IsDefined( typeof(CompilerGeneratedAttribute), false )
           && type.Name.IndexOf( "AnonymousType", StringComparison.Ordinal ) != -1
           && (type.Name.StartsWith( "<>", StringComparison.OrdinalIgnoreCase ) || type.Name.StartsWith( "VB$", StringComparison.OrdinalIgnoreCase ));
}