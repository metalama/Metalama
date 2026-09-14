// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Microsoft.CodeAnalysis.CSharp;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

/// <summary>
/// Builds the parts of the declaration of an introduced record that are specific to that kind.
/// </summary>
internal static class RecordHelper
{
    /// <summary>
    /// Builds the base list of a record that passes arguments to the primary constructor of its base record, or
    /// returns <c>null</c> when the record passes none and the caller builds an ordinary base list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The language writes those arguments in the base list, as in <c>record Derived( int X ) : BaseRecord( X )</c>,
    /// and the syntax model represents that form as a <see cref="PrimaryConstructorBaseTypeSyntax"/>.
    /// </para>
    /// </remarks>
    public static BaseListSyntax? GetBaseList( INamedType introducedType, RecordBuilderData builderData, MemberInjectionContext context )
    {
        if ( builderData.BaseArguments.IsDefaultOrEmpty || introducedType.BaseType == null )
        {
            return null;
        }

        var syntaxSerializationContext = new SyntaxSerializationContext(
            context.FinalCompilation,
            context.SyntaxGenerationContext,
            context.AspectReferenceSyntaxProvider,
            introducedType );

        var argumentList =
            ArgumentList(
                SeparatedList(
                    builderData.BaseArguments.Select(
                        a => Argument(
                            a.ParameterName != null ? NameColon( SyntaxFactoryEx.SafeIdentifierName( a.ParameterName ) ) : null,
                            default,
                            a.Expression.ToExpressionSyntax( syntaxSerializationContext ) ) ) ) );

        return BaseList(
            SingletonSeparatedList<BaseTypeSyntax>(
                PrimaryConstructorBaseType( context.SyntaxGenerator.TypeSyntax( introducedType.BaseType.ToNonNullable() ), argumentList ) ) );
    }

    /// <summary>
    /// Builds the positional parameter list of a record, which is the parameter list of its primary constructor, or
    /// returns <c>null</c> when the record declares no positional parameter and is therefore not positional.
    /// </summary>
    public static ParameterListSyntax? GetParameterList( INamedType introducedType, MemberInjectionContext context )
    {
        var primaryConstructor = introducedType.PrimaryConstructor;

        if ( primaryConstructor is not { Parameters.Count: > 0 } )
        {
            return null;
        }

        var parameterList = context.SyntaxGenerator.ParameterList( primaryConstructor, context.FinalCompilation );

        // The property that a positional parameter declares has no declaration of its own, so an attribute added to
        // that property is written on the parameter with the property target, which is the form the language
        // provides and the only place the attribute can go.
        var positionalProperties = introducedType.Facets.Record.AssertNotNull().PositionalProperties;

        if ( positionalProperties.Count != parameterList.Parameters.Count )
        {
            return parameterList;
        }

        var parameters = new ParameterSyntax[parameterList.Parameters.Count];
        var hasPropertyAttribute = false;

        for ( var i = 0; i < parameters.Length; i++ )
        {
            var parameter = parameterList.Parameters[i];
            var propertyAttributes = AdviceSyntaxGenerator.GetAttributeLists( positionalProperties[i], context, SyntaxKind.PropertyKeyword );

            if ( propertyAttributes.Count > 0 )
            {
                parameter = parameter.WithAttributeLists( parameter.AttributeLists.AddRange( propertyAttributes ) );
                hasPropertyAttribute = true;
            }

            parameters[i] = parameter;
        }

        return hasPropertyAttribute ? parameterList.WithParameters( SeparatedList( parameters ) ) : parameterList;
    }
}
