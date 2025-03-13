// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.CompilerServices;

namespace Flashtrace.Formatters;

internal static class ReflectionHelpers
{
    public static bool IsAnonymous( this Type type )
        => type.IsDefined( typeof(CompilerGeneratedAttribute), false )
           && type.Name.IndexOf( "AnonymousType", StringComparison.Ordinal ) != -1
           && (type.Name.StartsWith( "<>", StringComparison.OrdinalIgnoreCase ) || type.Name.StartsWith( "VB$", StringComparison.OrdinalIgnoreCase ));
}