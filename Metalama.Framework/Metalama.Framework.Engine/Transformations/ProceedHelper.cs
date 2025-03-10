// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.RunTime;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SpecialType = Metalama.Framework.Code.SpecialType;

namespace Metalama.Framework.Engine.Transformations;

internal static class ProceedHelper
{
    public static (ExpressionSyntax Syntax, IType Result) CreateProceedExpression(
        SyntaxGenerationContext generationContext,
        ExpressionSyntax invocationExpression,
        TemplateKind selectedTemplateKind,
        IMethod overriddenMethod )
    {
        TypeSyntax GetRuntimeAspectHelperTypeSyntax()
            => generationContext.SyntaxGenerator.TypeSyntax( generationContext.ReflectionMapper.GetTypeSymbol( typeof(RunTimeAspectHelper) ) );

        switch ( selectedTemplateKind )
        {
            case TemplateKind.Default when overriddenMethod.GetIteratorInfoImpl() is { IsIteratorMethod: true } iteratorInfo:
                {
                    // The target method is a yield-based iterator.

                    ExpressionSyntax expression;

                    if ( !(iteratorInfo.EnumerableKind is EnumerableKind.IAsyncEnumerable or EnumerableKind.IAsyncEnumerator) )
                    {
                        // The target method is a non-async iterator.
                        // Generate:  `RuntimeAspectHelper.Buffer( BASE(ARGS) )`

                        expression =
                            InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        GetRuntimeAspectHelperTypeSyntax(),
                                        IdentifierName( nameof(RunTimeAspectHelper.Buffer) ) ) )
                                .WithArgumentList( ArgumentList( SingletonSeparatedList( Argument( invocationExpression ) ) ) )
                                .WithSimplifierAnnotationIfNecessary( generationContext );
                    }
                    else
                    {
                        // The target method is an async iterator.
                        // Generate: `( await RuntimeAspectHelper.BufferAsync( BASE(ARGS) ) )` 

                        expression = GenerateAwaitBufferAsync();
                    }

                    return (expression, overriddenMethod.ReturnType);
                }

            case TemplateKind.Default when overriddenMethod.GetAsyncInfoImpl() is { IsAsync: true, IsAwaitableOrVoid: true } asyncInfo:
                {
                    // The target method is an async method (but not an async iterator).
                    // Generate (awaitable non-void): `( await BASE(ARGS) )`.
                    //           Or (awaitable void): `await BASE(ARGS)`.
                    //           Or (void)          : `await __LinkerInjectionHelpers__.__AsyncVoidMethod(BASE)(ARGS)`

                    switch ( asyncInfo )
                    {
                        case { } when overriddenMethod.ReturnType.IsConvertibleTo( SpecialType.Void ):
                            return WrapAsyncVoid( invocationExpression, overriddenMethod, true, generationContext );

                        case { ResultType: var resultType } when resultType.IsConvertibleTo( SpecialType.Void ):
                            return (
                                AwaitExpression(
                                        SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.AwaitKeyword ),
                                        invocationExpression )
                                    .WithSimplifierAnnotationIfNecessary( generationContext ),
                                resultType);

                        default:
                            return (
                                ParenthesizedExpression(
                                        AwaitExpression(
                                            SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.AwaitKeyword ),
                                            invocationExpression ) )
                                    .WithSimplifierAnnotationIfNecessary( generationContext ),
                                asyncInfo.ResultType);
                    }
                }

            case TemplateKind.Async when overriddenMethod.ReturnType.IsConvertibleTo( SpecialType.Void ):
                return WrapAsyncVoid( invocationExpression, overriddenMethod, false, generationContext );

            case TemplateKind.Async when overriddenMethod.GetIteratorInfoImpl() is
                { EnumerableKind: EnumerableKind.IAsyncEnumerable or EnumerableKind.IAsyncEnumerator }:
                {
                    var expression = GenerateAwaitBufferAsync();

                    return (expression, overriddenMethod.ReturnType);
                }
        }

        // This is a default method, or a non-default template.
        // Generate: `BASE(ARGS)`
        return (invocationExpression, overriddenMethod.ReturnType);

        ExpressionSyntax GenerateAwaitBufferAsync()
        {
            var arguments = ArgumentList( SingletonSeparatedList( Argument( invocationExpression ) ) );

            var cancellationTokenParameter = overriddenMethod.Parameters
                .OfParameterType<CancellationToken>()
                .LastOrDefault( p => p.Attributes.Any( a => a.Type.Name == "EnumeratorCancellationAttribute" ) );

            if ( cancellationTokenParameter != null )
            {
                arguments = arguments.AddArguments( Argument( IdentifierName( cancellationTokenParameter.Name ) ) );
            }

            var bufferExpression =
                InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            GetRuntimeAspectHelperTypeSyntax(),
                            IdentifierName( nameof(RunTimeAspectHelper.Buffer) + "Async" ) ) )
                    .WithArgumentList( arguments )
                    .WithSimplifierAnnotationIfNecessary( generationContext );

            var expression = ParenthesizedExpression(
                    AwaitExpression(
                        SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.AwaitKeyword ),
                        bufferExpression ) )
                .WithSimplifierAnnotationIfNecessary( generationContext );

            return expression;
        }
    }

    private static (ExpressionSyntax Syntax, IType Result) WrapAsyncVoid(
        ExpressionSyntax invocationExpression,
        IMethod overriddenMethod,
        bool await,
        SyntaxGenerationContext generationContext )
    {
        if ( invocationExpression is not InvocationExpressionSyntax { Expression: { } invocationTarget } actualInvocationExpression )
        {
            throw new AssertionFailedException( $"Expected invocation expression, got {invocationExpression.Kind()}" );
        }

        var expression =
            actualInvocationExpression.WithExpression(
                    InvocationExpression(
                        LinkerInjectionHelperProvider.GetAsyncVoidMethodMemberExpression(),
                        ArgumentList( SingletonSeparatedList( Argument( invocationTarget ) ) ) ) )
                .WithSimplifierAnnotationIfNecessary( generationContext );

        if ( await )
        {
            return (
                AwaitExpression(
                    Token( TriviaList(), SyntaxKind.AwaitKeyword, TriviaList( ElasticSpace ) ),
                    expression ),
                overriddenMethod.ReturnType);
        }
        else
        {
            return (expression, overriddenMethod.ReturnType);
        }
    }

    public static SyntaxUserExpression CreateProceedDynamicExpression(
        SyntaxGenerationContext generationContext,
        ExpressionSyntax invocationExpression,
        TemplateKind selectedTemplateKind,
        IMethod overriddenMethod )
    {
        var (expression, type) = CreateProceedExpression( generationContext, invocationExpression, selectedTemplateKind, overriddenMethod );

        return new SyntaxUserExpression( expression, type );
    }

    public static ExpressionSyntax CreateMemberAccessExpression(
        IMember targetMember,
        AspectLayerId aspectLayerId,
        AspectReferenceTargetKind referenceTargetKind,
        SyntaxGenerationContext generationContext )
    {
        ExpressionSyntax expression;

        var memberNameString =
            targetMember switch
            {
                { IsExplicitInterfaceImplementation: true } => targetMember.Name.Split( '.' ).Last(),
                _ => targetMember.Name
            };

        SimpleNameSyntax memberName;

        if ( targetMember is IGeneric { TypeParameters.Count: > 0 } generic )
        {
            memberName = GenericName(
                Identifier( memberNameString ),
                TypeArgumentList( SeparatedList( generic.TypeParameters.SelectAsReadOnlyList( p => (TypeSyntax) IdentifierName( p.Name ) ) ) ) );
        }
        else
        {
            memberName = IdentifierName( memberNameString );
        }

        if ( !targetMember.IsStatic )
        {
            if ( targetMember.IsExplicitInterfaceImplementation )
            {
                var implementedInterfaceMember = targetMember.GetExplicitInterfaceImplementation();

                expression = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ParenthesizedExpression(
                        generationContext.SyntaxGenerator.SafeCastExpression(
                            generationContext.SyntaxGenerator.TypeSyntax( implementedInterfaceMember.DeclaringType ),
                            ThisExpression() ) ),
                    memberName );
            }
            else
            {
                expression = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    memberName );
            }
        }
        else
        {
            expression =
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    generationContext.SyntaxGenerator.TypeExpression( targetMember.DeclaringType ),
                    memberName );
        }

        return expression
            .WithAspectReferenceAnnotation(
                aspectLayerId,
                AspectReferenceOrder.Previous,
                referenceTargetKind,
                AspectReferenceFlags.Inlineable );
    }
}