// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking;

internal sealed class LinkerAspectReferenceSyntaxProvider : AspectReferenceSyntaxProvider
{
    public override ExpressionSyntax GetFinalizerReference( AspectLayerId aspectLayer )
        => InvocationExpression(
            LinkerInjectionHelperProvider.GetFinalizeMemberExpression()
                .WithAspectReferenceAnnotation(
                    aspectLayer,
                    AspectReferenceOrder.Previous,
                    flags: AspectReferenceFlags.Inlineable ) );

    public override ExpressionSyntax GetStaticConstructorReference( AspectLayerId aspectLayer )
        => InvocationExpression(
            LinkerInjectionHelperProvider.GetStaticConstructorMemberExpression()
                .WithAspectReferenceAnnotation(
                    aspectLayer,
                    AspectReferenceOrder.Previous,
                    flags: AspectReferenceFlags.Inlineable ),
            ArgumentList() );

    public override ExpressionSyntax GetConstructorReference(
        AspectLayerId aspectLayer,
        IConstructor overriddenConstructor,
        ContextualSyntaxGenerator syntaxGenerator )
        => InvocationExpression(
            LinkerInjectionHelperProvider.GetConstructorMemberExpression()
                .WithAspectReferenceAnnotation(
                    aspectLayer,
                    AspectReferenceOrder.Previous,
                    flags: AspectReferenceFlags.Inlineable ),
            ArgumentList(
                SingletonSeparatedList(
                    Argument(
                        ObjectCreationExpression(
                            syntaxGenerator.TypeSyntax( overriddenConstructor.DeclaringType ),
                            ArgumentList(
                                SeparatedList(
                                    overriddenConstructor.Parameters
                                        .SelectAsArray(
                                            p =>
                                                Argument(
                                                    NameColon( IdentifierName( p.Name ) ),
                                                    p.RefKind.InvocationRefKindToken(),
                                                    IdentifierName( p.Name ) ) ) ) ),
                            null ) ) ) ) );

    public override ExpressionSyntax GetPropertyReference(
        AspectLayerId aspectLayer,
        IProperty targetProperty,
        AspectReferenceTargetKind targetKind,
        ContextualSyntaxGenerator syntaxGenerator )
    {
        switch (targetKind, targetProperty)
        {
            case (AspectReferenceTargetKind.PropertySetAccessor, { SetMethod: IDeclarationImpl { ImplementationKind: DeclarationImplementationKind.Pseudo } }):
            case (AspectReferenceTargetKind.PropertyGetAccessor, { GetMethod: IDeclarationImpl { ImplementationKind: DeclarationImplementationKind.Pseudo } }):
                // For pseudo source: __LinkerInjectionHelpers__.__Property(<property_expression>)
                // It is important to track the <property_expression>.
                var symbolSourceExpression = CreateMemberAccessExpression( targetProperty, syntaxGenerator );

                return
                    InvocationExpression(
                        LinkerInjectionHelperProvider.GetPropertyMemberExpression()
                            .WithAspectReferenceAnnotation(
                                aspectLayer,
                                AspectReferenceOrder.Previous,
                                targetKind,
                                AspectReferenceFlags.Inlineable ),
                        ArgumentList( SingletonSeparatedList( Argument( symbolSourceExpression ) ) ) );

            default:
                // Otherwise: <property_expression>
                return
                    CreateMemberAccessExpression( targetProperty, syntaxGenerator )
                        .WithAspectReferenceAnnotation(
                            aspectLayer,
                            AspectReferenceOrder.Previous,
                            targetKind,
                            AspectReferenceFlags.Inlineable );
        }
    }

    public override ExpressionSyntax GetIndexerReference(
        AspectLayerId aspectLayer,
        IIndexer targetIndexer,
        AspectReferenceTargetKind targetKind,
        ContextualSyntaxGenerator syntaxGenerator )
        => ElementAccessExpression(
                CreateIndexerAccessExpression( targetIndexer, syntaxGenerator ),
                BracketedArgumentList(
                    SeparatedList(
                        targetIndexer.Parameters.SelectAsReadOnlyList(
                            p => Argument( null, p.RefKind.InvocationRefKindToken(), IdentifierName( p.Name ) ) ) ) ) )
            .WithAspectReferenceAnnotation(
                aspectLayer,
                AspectReferenceOrder.Previous,
                targetKind,
                AspectReferenceFlags.Inlineable );

    public override ExpressionSyntax GetOperatorReference( AspectLayerId aspectLayer, IMethod targetOperator, ContextualSyntaxGenerator syntaxGenerator )
        => InvocationExpression(
            LinkerInjectionHelperProvider.GetOperatorMemberExpression(
                    syntaxGenerator,
                    targetOperator.OperatorKind,
                    targetOperator.ReturnType,
                    targetOperator.Parameters.SelectAsReadOnlyList( p => p.Type ) )
                .WithAspectReferenceAnnotation(
                    aspectLayer,
                    AspectReferenceOrder.Previous,
                    flags: AspectReferenceFlags.Inlineable ),
            syntaxGenerator.ArgumentList( targetOperator, p => IdentifierName( p.Name ) ) );

    public override ExpressionSyntax GetEventFieldInitializerExpression( TypeSyntax eventFieldType, ExpressionSyntax initializerExpression )
        => InvocationExpression(
            LinkerInjectionHelperProvider.GetEventFieldInitializerExpressionMemberExpression( eventFieldType ),
            ArgumentList( SingletonSeparatedList( Argument( null, default, initializerExpression ) ) ) );

    private static ExpressionSyntax CreateIndexerAccessExpression( IIndexer targetIndexer, ContextualSyntaxGenerator syntaxGenerator )
    {
        ExpressionSyntax expression;

        if ( targetIndexer.IsExplicitInterfaceImplementation )
        {
            var implementedInterfaceMember = targetIndexer.GetExplicitInterfaceImplementation();

            expression =
                ParenthesizedExpression(
                    syntaxGenerator.SafeCastExpression(
                        syntaxGenerator.TypeSyntax( implementedInterfaceMember.DeclaringType ),
                        ThisExpression() ) );
        }
        else
        {
            expression = ThisExpression();
        }

        return expression;
    }

    private static ExpressionSyntax CreateMemberAccessExpression( IMember targetDeclaration, ContextualSyntaxGenerator syntaxGenerator )
    {
        ExpressionSyntax expression;

        var memberNameString =
            targetDeclaration switch
            {
                { IsExplicitInterfaceImplementation: true } => targetDeclaration.Name.Split( '.' ).Last(),
                _ => targetDeclaration.Name
            };

        SimpleNameSyntax memberName;

        if ( targetDeclaration is IGeneric { TypeParameters.Count: > 0 } generic )
        {
            memberName = GenericName( memberNameString )
                .WithTypeArgumentList(
                    TypeArgumentList( SeparatedList( generic.TypeParameters.SelectAsReadOnlyList( p => (TypeSyntax) IdentifierName( p.Name ) ) ) ) );
        }
        else
        {
            memberName = IdentifierName( memberNameString );
        }

        if ( !targetDeclaration.IsStatic )
        {
            if ( targetDeclaration.IsExplicitInterfaceImplementation )
            {
                var implementedInterfaceMember = targetDeclaration.GetExplicitInterfaceImplementation();

                expression = MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ParenthesizedExpression(
                            syntaxGenerator.SafeCastExpression(
                                syntaxGenerator.TypeSyntax( implementedInterfaceMember.DeclaringType ),
                                ThisExpression() ) ),
                        memberName )
                    .WithSimplifierAnnotationIfNecessary( syntaxGenerator.SyntaxGenerationContext );
            }
            else
            {
                expression = MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ThisExpression(),
                        memberName )
                    .WithSimplifierAnnotationIfNecessary( syntaxGenerator.SyntaxGenerationContext );
            }
        }
        else
        {
            expression =
                MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        syntaxGenerator.TypeExpression( targetDeclaration.DeclaringType ),
                        memberName )
                    .WithSimplifierAnnotationIfNecessary( syntaxGenerator.SyntaxGenerationContext );
        }

        return expression;
    }
}