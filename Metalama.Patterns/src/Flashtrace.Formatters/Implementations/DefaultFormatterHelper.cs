// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Concurrent;
using System.Reflection;

namespace Flashtrace.Formatters.Implementations;

internal static class DefaultFormatterHelper
{
    private static readonly ConcurrentDictionary<Type, bool> _hasCustomToStringMethod = new();

    public static bool HasCustomToStringMethod( Type type )
    {
        return _hasCustomToStringMethod.GetOrAdd(
            type,
            t =>
                t.GetMethod( "ToString", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null )?.DeclaringType != typeof(object) );
    }
}