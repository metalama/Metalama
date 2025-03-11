// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Globalization;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

internal static class IdentifierHelper
{
    /// <summary>
    /// Returns true if the Unicode character can be a part of an identifier.
    /// </summary>
    /// <param name="ch">The Unicode character.</param>
    private static bool IsIdentifierPartCharacter( char ch )
    {
        switch ( ch )
        {
            case < 'a' and < 'A':
                return ch is >= '0' and <= '9';

            case < 'a':
                return ch is <= 'Z' or '_';

            case <= 'z':
                return true;

            case <= '\u007F':
                return false;

            default:
                {
                    var cat = CharUnicodeInfo.GetUnicodeCategory( ch );

                    return IsLetterChar( cat )
                           || IsDecimalDigitChar( cat )
                           || IsConnectingChar( cat )
                           || IsCombiningChar( cat )
                           || IsFormattingChar( cat );
                }
        }
    }

    private static bool IsLetterChar( UnicodeCategory cat )
    {
        // letter-character:
        //   A Unicode character of classes Lu, Ll, Lt, Lm, Lo, or Nl 
        //   A Unicode-escape-sequence representing a character of classes Lu, Ll, Lt, Lm, Lo, or Nl

        switch ( cat )
        {
            case UnicodeCategory.UppercaseLetter:
            case UnicodeCategory.LowercaseLetter:
            case UnicodeCategory.TitlecaseLetter:
            case UnicodeCategory.ModifierLetter:
            case UnicodeCategory.OtherLetter:
            case UnicodeCategory.LetterNumber:
                return true;
        }

        return false;
    }

    private static bool IsCombiningChar( UnicodeCategory cat )
    {
        // combining-character:
        //   A Unicode character of classes Mn or Mc 
        //   A Unicode-escape-sequence representing a character of classes Mn or Mc

        switch ( cat )
        {
            case UnicodeCategory.NonSpacingMark:
            case UnicodeCategory.SpacingCombiningMark:
                return true;
        }

        return false;
    }

    private static bool IsDecimalDigitChar( UnicodeCategory cat )
    {
        // decimal-digit-character:
        //   A Unicode character of the class Nd 
        //   A unicode-escape-sequence representing a character of the class Nd

        return cat == UnicodeCategory.DecimalDigitNumber;
    }

    private static bool IsConnectingChar( UnicodeCategory cat )
    {
        // connecting-character:  
        //   A Unicode character of the class Pc
        //   A unicode-escape-sequence representing a character of the class Pc

        return cat == UnicodeCategory.ConnectorPunctuation;
    }

    /// <summary>
    /// Returns true if the Unicode character is a formatting character (Unicode class Cf).
    /// </summary>
    /// <param name="cat">The Unicode character.</param>
    private static bool IsFormattingChar( UnicodeCategory cat )
    {
        // formatting-character:  
        //   A Unicode character of the class Cf
        //   A unicode-escape-sequence representing a character of the class Cf

        return cat == UnicodeCategory.Format;
    }

    public static void ValidateSyntaxTreeName( string name )
    {
        foreach ( var c in name )
        {
            if ( !IsIdentifierPartCharacter( c )
                 && c != '.'
                 && c != ','
                 && c != '-'
                 && c != '_'
                 && c != ' '
                 && c != '('
                 && c != ')'
                 && c != '['
                 && c != ']'
                 && c != '{'
                 && c != '}' )
            {
                throw new ArgumentException( $"The identifier '{name}' contains the character '{c}', which is not allowed." );
            }
        }
    }
}