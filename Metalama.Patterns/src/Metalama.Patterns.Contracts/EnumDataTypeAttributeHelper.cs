// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Globalization;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Provides runtime helper methods for validating enum values, used by <see cref="EnumDataTypeAttribute"/>.
/// </summary>
/// <seealso cref="EnumDataTypeAttribute"/>
[PublicAPI]
public static class EnumDataTypeAttributeHelper
{
    /// <summary>
    /// Determines whether the specified string value is a valid member of the specified enumeration type.
    /// </summary>
    /// <param name="value">The string value to validate. Null or empty strings are considered valid.</param>
    /// <param name="enumType">The enumeration type to validate against.</param>
    /// <returns><c>true</c> if the value is valid for the enumeration; otherwise, <c>false</c>.</returns>
    public static bool IsValidEnumValue( string value, Type enumType )
    {
        if ( string.IsNullOrEmpty( value ) )
        {
            return true;
        }

        object enumValue;

        try
        {
            enumValue = Enum.Parse( enumType, value, false );
        }
        catch ( ArgumentException )
        {
            return false;
        }

        return IsValidEnumValueCore( enumValue, enumType );
    }

    /// <summary>
    /// Determines whether the specified object value is a valid member of the specified enumeration type.
    /// </summary>
    /// <param name="value">The value to validate. Null values are considered valid. If the value is a string, it is matched against enum member names.</param>
    /// <param name="enumType">The enumeration type to validate against.</param>
    /// <returns><c>true</c> if the value is valid for the enumeration; otherwise, <c>false</c>.</returns>
    public static bool IsValidEnumValue( object? value, Type enumType )
    {
        switch ( value )
        {
            case null:
                return true;

            case string str:
                return IsValidEnumValue( str, enumType );
        }

        var type = value.GetType();

        if ( !type.IsEnum || enumType != type )
        {
            return false;
        }

        return IsValidEnumValueCore( value, enumType );
    }

    private static bool IsValidEnumValueCore( object value, Type enumType )
    {
        if ( IsEnumTypeInFlagsMode( enumType ) )
        {
            if ( !GetUnderlyingTypeValueString( enumType, value ).Equals( value.ToString(), StringComparison.Ordinal ) )
            {
                return true;
            }

            return false;
        }
        else
        {
            return Enum.IsDefined( enumType, value );
        }
    }

    private static bool IsEnumTypeInFlagsMode( Type enumType ) => enumType.GetCustomAttributes( typeof(FlagsAttribute), false ).Length != 0;

    // VS gives incorrect warning at build time, but no squiggly, hence the double suppression.
#pragma warning disable IDE0079 // Remove unnecessary suppression

// ReSharper disable once RedundantSuppressNullableWarningExpression
    private static string GetUnderlyingTypeValueString( Type enumType, object enumValue )
        => Convert.ChangeType( enumValue, Enum.GetUnderlyingType( enumType ), CultureInfo.InvariantCulture )
            .ToString()!;
#pragma warning restore IDE0079 // Remove unnecessary suppression
}