// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.CSharp;
using System;

namespace Metalama.Framework.Engine.Extensibility.Synthesis;

/// <summary>
/// Validates the names of the declarations that a pipeline extension declares.
/// </summary>
internal static class SynthesisNames
{
    /// <summary>
    /// Returns a name after checking that it is a valid C# identifier that is not a keyword.
    /// </summary>
    /// <exception cref="ArgumentException">The name is not a valid identifier.</exception>
    public static string ValidateIdentifier( string? name, string parameterName )
    {
        if ( string.IsNullOrEmpty( name ) || !SyntaxFacts.IsValidIdentifier( name ) || SyntaxFacts.GetKeywordKind( name! ) != SyntaxKind.None )
        {
            throw new ArgumentException( $"The name '{name}' is not a valid identifier.", parameterName );
        }

        return name!;
    }
}
