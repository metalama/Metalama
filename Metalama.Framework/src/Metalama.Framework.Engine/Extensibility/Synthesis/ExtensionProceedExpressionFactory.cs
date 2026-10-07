// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SpecialType = Metalama.Framework.Code.SpecialType;

namespace Metalama.Framework.Engine.Extensibility.Synthesis;

/// <summary>
/// Creates the expression of <c>meta.Proceed()</c> in a method that a pipeline extension declares from a template, according to a
/// <see cref="ProceedBinding"/>.
/// </summary>
internal static class ExtensionProceedExpressionFactory
{
    /// <summary>
    /// Creates the expression of <c>meta.Proceed()</c>.
    /// </summary>
    /// <param name="binding">The binding of the declared method.</param>
    /// <param name="declaredMethod">The declared method.</param>
    /// <param name="templateKind">The kind of the template that is expanded.</param>
    /// <param name="context">The syntax generation context.</param>
    public static SyntaxUserExpression Create(
        ProceedBinding binding,
        IMethod declaredMethod,
        TemplateKind templateKind,
        SyntaxGenerationContext context )
    {
        var invocation = CreateInvocation( binding, declaredMethod, context );

        // A void method is not awaited, whatever the kind of the template: the invoked method may be an 'async void' method of another assembly, which
        // cannot be awaited.
        if ( declaredMethod.ReturnType.SpecialType == SpecialType.Void && (templateKind == TemplateKind.Async || declaredMethod.IsAsync) )
        {
            return new SyntaxUserExpression( invocation, declaredMethod.ReturnType );
        }

        var (syntax, type) = ProceedHelper.CreateProceedExpression( context, invocation, templateKind, declaredMethod );

        return new SyntaxUserExpression( syntax, type );
    }

    /// <summary>
    /// Creates the invocation of the invoked method, with the parameters of the declared method as arguments.
    /// </summary>
    internal static InvocationExpressionSyntax CreateInvocation( ProceedBinding binding, IMethod declaredMethod, SyntaxGenerationContext context )
    {
        var method = binding.Method;

        var name = binding.TypeArguments.IsEmpty
            ? (SimpleNameSyntax) SyntaxFactoryEx.SafeIdentifierName( method.Name )
            : GenericName(
                SyntaxFactoryEx.SafeIdentifier( method.Name ),
                TypeArgumentList( SeparatedList( binding.TypeArguments.Select( t => context.SyntaxGenerator.TypeSyntax( t ) ) ) ) );

        var receiver = binding.Kind switch
        {
            ProceedBindingKind.InvokeStatic => context.SyntaxGenerator.TypeExpression( method.DeclaringType ),
            ProceedBindingKind.InvokeOnParameter => SyntaxFactoryEx.SafeIdentifierName( GetParameter( declaredMethod, binding.ReceiverParameterIndex ).Name ),
            _ => throw new AssertionFailedException( $"Unexpected binding kind {binding.Kind}." )
        };

        var callee = MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, receiver, name ).WithSimplifierAnnotationIfNecessary( context );

        var parameterIndices = GetArgumentParameterIndices( binding, declaredMethod );
        var arguments = new List<ArgumentSyntax>( method.Parameters.Count );

        for ( var i = 0; i < method.Parameters.Count; i++ )
        {
            var parameter = GetParameter( declaredMethod, parameterIndices[i] );
            ExpressionSyntax argument = SyntaxFactoryEx.SafeIdentifierName( parameter.Name );

            foreach ( var cast in binding.ArgumentCasts )
            {
                if ( cast.ParameterIndex == parameterIndices[i] )
                {
                    argument = ParenthesizedExpression( CastExpression( context.SyntaxGenerator.TypeSyntax( cast.Type ), argument ) )
                        .WithSimplifierAnnotationIfNecessary( context );
                }
            }

            arguments.Add( Argument( null, method.Parameters[i].RefKind.InvocationRefKindToken(), argument ) );
        }

        return InvocationExpression( callee, ArgumentList( SeparatedList( arguments ) ) );
    }

    /// <summary>
    /// Returns, for each parameter of the invoked method, the index of the parameter of the declared method that is passed.
    /// </summary>
    private static IReadOnlyList<int> GetArgumentParameterIndices( ProceedBinding binding, IMethod declaredMethod )
    {
        if ( !binding.ArgumentParameterIndices.IsDefault )
        {
            if ( binding.ArgumentParameterIndices.Length != binding.Method.Parameters.Count )
            {
                throw new AssertionFailedException( "The number of argument parameter indices does not match the number of parameters." );
            }

            return binding.ArgumentParameterIndices;
        }

        return Enumerable.Range( 0, declaredMethod.Parameters.Count )
            .Where( i => i != binding.ReceiverParameterIndex )
            .Take( binding.Method.Parameters.Count )
            .ToList();
    }

    /// <summary>
    /// Returns a parameter of the declared method.
    /// </summary>
    private static IParameter GetParameter( IMethod declaredMethod, int index )
        => index >= 0 && index < declaredMethod.Parameters.Count
            ? declaredMethod.Parameters[index]
            : throw new AssertionFailedException( $"The parameter index {index} is out of range for '{declaredMethod}'." );
}
