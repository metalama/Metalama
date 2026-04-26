// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Engine.Utilities;

[PublicAPI]
public static class EnumExtensions
{
    /// <summary>
    /// Returns a deterministic string representation of an enum value. For enums with alias members
    /// (multiple names sharing the same underlying value, e.g. <c>Default = 0, Success = 0</c>), the
    /// built-in <see cref="object.ToString"/> picks one of the names in an implementation-defined way
    /// that can vary across .NET runtimes. This method always returns the alphabetically-first name
    /// among aliases, so diagnostic messages and log output stay stable.
    /// </summary>
    public static string ToStringSafe( this Enum value )
    {
        var type = value.GetType();
        string? bestName = null;

        foreach ( var name in Enum.GetNames( type ) )
        {
            var parsed = (Enum) Enum.Parse( type, name );

            if ( parsed.Equals( value ) && (bestName == null || string.CompareOrdinal( name, bestName ) < 0) )
            {
                bestName = name;
            }
        }

        return bestName ?? value.ToString();
    }
}
