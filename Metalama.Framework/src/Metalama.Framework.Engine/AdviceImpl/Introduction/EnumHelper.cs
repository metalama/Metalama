// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SpecialType = Metalama.Framework.Code.SpecialType;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

/// <summary>
/// Builds the parts of the declaration of an introduced enum that are specific to that kind.
/// </summary>
internal static class EnumHelper
{
    /// <summary>
    /// Builds the base list of an enum, which is its underlying integral type. The list is omitted when that type is
    /// <c>int</c>, which the language implies.
    /// </summary>
    public static BaseListSyntax? GetBaseList( INamedType introducedType, MemberInjectionContext context )
    {
        var underlyingType = introducedType.UnderlyingType;

        return underlyingType.SpecialType == SpecialType.Int32
            ? null
            : BaseList( SingletonSeparatedList<BaseTypeSyntax>( SimpleBaseType( context.SyntaxGenerator.TypeSyntax( underlyingType ) ) ) );
    }

    /// <summary>
    /// Builds the members of an enum, in the order in which the aspect added them, each with the value it was given
    /// or none when the language assigns it.
    /// </summary>
    public static IEnumerable<EnumMemberDeclarationSyntax> GetMembers( INamedType introducedType, MemberInjectionContext context )
    {
        // The order is taken from the facet and not from INamedType.Fields, because the order of that collection
        // depends on which fields a previous consumer resolved by name, while the members of an enum are emitted in
        // the order in which the aspect added them.
        foreach ( var field in introducedType.Facets.Enum.AssertNotNull().Members )
        {
            var value = field.ConstantValue;

            var equalsValue =
                value is { IsInitialized: true, Value: not null }
                    ? EqualsValueClause( context.SyntaxGenerator.TypedConstant( value.Value ) )
                    : null;

            yield return
                EnumMemberDeclaration(
                    AdviceSyntaxGenerator.GetAttributeLists( field, context ),
                    default,
                    SyntaxFactoryEx.SafeIdentifier( field.Name ),
                    equalsValue );
        }
    }
}
