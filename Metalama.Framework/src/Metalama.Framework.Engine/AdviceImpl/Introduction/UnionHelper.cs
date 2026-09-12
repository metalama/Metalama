// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_11_0_OR_GREATER
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

/// <summary>
/// Builds the parts of the declaration of an introduced union that are specific to that kind.
/// </summary>
/// <remarks>
/// <para>
/// The whole class is compiled into the latest Roslyn variant only, because the other one cannot emit a union
/// declaration at all.
/// </para>
/// </remarks>
internal static class UnionHelper
{
    /// <summary>
    /// Builds the case list of a union, which the syntax model represents as a parameter list whose parameters carry
    /// a type and no identifier.
    /// </summary>
    public static ParameterListSyntax GetCaseList( INamedType introducedType, MemberInjectionContext context )
        => ParameterList(
            SeparatedList(
                introducedType.Facets.Union.AssertNotNull()
                    .Cases.SelectAsReadOnlyList(
                        c => Parameter(
                            List<AttributeListSyntax>(),
                            default,
                            context.SyntaxGenerator.TypeSyntax( c.Type ),
                            default,
                            null ) ) ) );
}
#endif
