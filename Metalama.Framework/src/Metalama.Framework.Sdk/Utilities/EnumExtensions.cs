// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Utilities;

[PublicAPI]
public static class EnumExtensions
{
    // Per-type cache of value-to-deterministic-name mappings. The diagnostic formatter is a hot path,
    // so a lookup beats re-enumerating Enum.GetNames + Enum.Parse on every call.
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<Enum, string>> _cache = new();

    /// <summary>
    /// Returns a deterministic string representation of an enum value. For enums with alias members
    /// (multiple names sharing the same underlying value, e.g. <c>Default = 0, Success = 0</c>), the
    /// built-in <see cref="object.ToString"/> picks one of the names in an implementation-defined way
    /// that can vary across .NET runtimes. This method returns the alphabetically-first preferred
    /// name among aliases — names marked <c>[Obsolete]</c> and the literal name
    /// <c>Default</c> are deprioritized and only used as a fallback when no preferred alias exists,
    /// so diagnostic messages and log output stay stable.
    /// </summary>
    public static string ToStringSafe( this Enum value )
    {
        var map = _cache.GetOrAdd( value.GetType(), BuildMap );

        return map.TryGetValue( value, out var name ) ? name : value.ToString();
    }

    private static IReadOnlyDictionary<Enum, string> BuildMap( Type type )
    {
        var names = Enum.GetNames( type );
        var entries = new (string Name, bool IsDeprioritized)[names.Length];

        for ( var i = 0; i < names.Length; i++ )
        {
            var name = names[i];
            var isObsolete = type.GetField( name )?.IsDefined( typeof(ObsoleteAttribute), inherit: false ) == true;
            entries[i] = (name, isObsolete || name == "Default");
        }

        // Preferred names sort before deprioritized ones; ties broken by ordinal name comparison.
        Array.Sort(
            entries,
            ( a, b ) =>
            {
                if ( a.IsDeprioritized != b.IsDeprioritized )
                {
                    return a.IsDeprioritized ? 1 : -1;
                }

                return string.CompareOrdinal( a.Name, b.Name );
            } );

        var result = new Dictionary<Enum, string>();

        foreach ( var (name, _) in entries )
        {
            var parsed = (Enum) Enum.Parse( type, name );

            if ( !result.ContainsKey( parsed ) )
            {
                result[parsed] = name;
            }
        }

        return result;
    }
}
