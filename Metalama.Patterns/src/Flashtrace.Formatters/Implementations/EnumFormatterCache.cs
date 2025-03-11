// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Flashtrace.Formatters.Implementations;

internal static class EnumFormatterCache<T>
    where T : Enum
{
    // To make this formatter efficient (i.e. to avoid allocations) and thread-safe,
    // names of named values of the enum are stored in simpleNames, which is never mutated.
    // Other names (bitwise ORs for [Flags], unnamed values) are cached per thread in otherNames.

    private static readonly Dictionary<T, string> _simpleNames;

    [ThreadStatic]
    private static Dictionary<T, string>? _otherNames;

    static EnumFormatterCache()
    {
        var values = (T[]) Enum.GetValues( typeof(T) );

        // Distinct is required, because GetValues() returns duplicates for values with multiple names
        _simpleNames = values.Distinct().ToDictionary( v => v, v => v.ToString() );
    }

    public static void Write( UnsafeStringBuilder stringBuilder, T? value )
    {
        stringBuilder.Append( GetString( value ) );
    }

    /// <summary>
    /// Returns the string value of the given enum value.
    /// </summary>
    public static string GetString( T? value )
    {
        if ( value == null )
        {
            throw new ArgumentNullException( nameof(value) );
        }

        if ( _simpleNames.TryGetValue( value, out var name ) )
        {
            return name;
        }

        _otherNames ??= new Dictionary<T, string>();

        if ( _otherNames.TryGetValue( value, out name ) )
        {
            return name;
        }

        return _otherNames[value] = value.ToString();
    }
}