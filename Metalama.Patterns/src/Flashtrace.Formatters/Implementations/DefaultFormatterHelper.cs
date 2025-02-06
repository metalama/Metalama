// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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