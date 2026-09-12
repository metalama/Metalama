// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

/// <summary>
/// Builds the parts of the declaration of an introduced record that are specific to that kind.
/// </summary>
internal static class RecordHelper
{
    /// <summary>
    /// Builds the positional parameter list of a record, which is the parameter list of its primary constructor, or
    /// returns <c>null</c> when the record declares no positional parameter and is therefore not positional.
    /// </summary>
    public static ParameterListSyntax? GetParameterList( INamedType introducedType, MemberInjectionContext context )
    {
        var primaryConstructor = introducedType.PrimaryConstructor;

        return primaryConstructor is not { Parameters.Count: > 0 }
            ? null
            : context.SyntaxGenerator.ParameterList( primaryConstructor, context.FinalCompilation );
    }
}
