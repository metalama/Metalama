// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.CodeModel.Invokers;

/// <summary>
/// An <see cref="UserExpression"/> that represents a method referenced as a delegate.
/// The generated syntax depends on the target type (when available) and on whether the method has overloads.
/// </summary>
internal sealed class MethodDelegateUserExpression : UserExpression
{
    private readonly IMethod _method;
    private readonly MethodInvoker _invoker;
    private readonly bool _hasOverloads;

    public MethodDelegateUserExpression( IMethod method, MethodInvoker invoker, bool hasOverloads, IType defaultDelegateType )
    {
        this._method = method;
        this._invoker = invoker;
        this._hasOverloads = hasOverloads;
        this.Type = defaultDelegateType;
    }

    public override IType Type { get; }

    protected override bool CanBeNull => false;

    protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
    {
        var methodGroupExpression = this.BuildMethodGroupExpression( syntaxSerializationContext );

        if ( !this._hasOverloads )
        {
            // No overloads: a simple method group expression is always sufficient.
            // If targetType is a delegate, the C# compiler will handle the conversion.
            return methodGroupExpression;
        }

        // There are overloads, so we need to disambiguate.
        // First, try using the target type if it's a compatible delegate type.
        if ( targetType is INamedType { TypeKind: TypeKind.Delegate } targetDelegateType
             && this.IsCompatibleWithDelegate( targetDelegateType ) )
        {
            // The target type is a delegate that matches our method's signature.
            // Generate: new TargetDelegateType(methodGroup)
            return CreateDelegateCreationExpression( methodGroupExpression, targetDelegateType, syntaxSerializationContext );
        }

        // No usable target type. Fall back to Action<>/Func<>.
        return this.CreateActionOrFuncExpression( methodGroupExpression, syntaxSerializationContext );
    }

    private ExpressionSyntax BuildMethodGroupExpression( SyntaxSerializationContext context )
    {
        SimpleNameSyntax name;

        if ( this._method.IsGeneric )
        {
            name = GenericName(
                SyntaxFactoryEx.SafeIdentifier( this._invoker.GetCleanTargetMemberName() ),
                TypeArgumentList(
                    SeparatedList( this._method.TypeArguments.SelectAsImmutableArray( t => context.SyntaxGenerator.TypeSyntax( t ) ) ) ) );
        }
        else
        {
            name = SyntaxFactoryEx.SafeIdentifierName( this._invoker.GetCleanTargetMemberName() );
        }

        var receiverInfo = this._invoker.GetReceiverInfo( context );
        var receiverSyntax = receiverInfo.GetReceiverSyntax( this._method, context );

        ExpressionSyntax methodGroupExpression =
            MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, receiverSyntax, name )
                .WithSimplifierAnnotationIfNecessary( context.SyntaxGenerationContext );

        // Add aspect reference annotation if applicable.
        if ( Invoker<IMethod>.GetTargetType()?.IsConvertibleTo( this._method.DeclaringType ) ?? false )
        {
            methodGroupExpression = methodGroupExpression.WithAspectReferenceAnnotation(
                receiverInfo.WithSyntax( receiverSyntax ).AspectReferenceSpecification.WithTargetKind( AspectReferenceTargetKind.Self ) );
        }

        return methodGroupExpression;
    }

    private bool IsCompatibleWithDelegate( INamedType delegateType )
    {
        var invokeMethod = delegateType.Methods.OfName( "Invoke" ).SingleOrDefault();

        if ( invokeMethod == null )
        {
            return false;
        }

        // Check parameter count.
        if ( invokeMethod.Parameters.Count != this._method.Parameters.Count )
        {
            return false;
        }

        // Check return type compatibility.
        var isVoid = this._method.ReturnType.SpecialType == SpecialType.Void;
        var delegateIsVoid = invokeMethod.ReturnType.SpecialType == SpecialType.Void;

        if ( isVoid != delegateIsVoid )
        {
            return false;
        }

        if ( !isVoid )
        {
            // The method's return type must be convertible to the delegate's return type.
            if ( !this._method.Compilation.Comparers.Default.IsConvertibleTo( this._method.ReturnType, invokeMethod.ReturnType ) )
            {
                return false;
            }
        }

        // Check parameter types and ref kinds.
        for ( var i = 0; i < this._method.Parameters.Count; i++ )
        {
            var methodParam = this._method.Parameters[i];
            var delegateParam = invokeMethod.Parameters[i];

            // Ref kinds must match exactly.
            if ( methodParam.RefKind != delegateParam.RefKind )
            {
                return false;
            }

            // Parameter types must be compatible.
            if ( !this._method.Compilation.Comparers.Default.IsConvertibleTo( delegateParam.Type, methodParam.Type ) )
            {
                return false;
            }
        }

        return true;
    }

    private static ExpressionSyntax CreateDelegateCreationExpression(
        ExpressionSyntax methodGroupExpression,
        INamedType delegateType,
        SyntaxSerializationContext context )
    {
        var delegateTypeSyntax = context.SyntaxGenerator.TypeSyntax( delegateType );

        return ObjectCreationExpression( delegateTypeSyntax )
            .WithArgumentList(
                ArgumentList(
                    SingletonSeparatedList(
                        Argument( methodGroupExpression ) ) ) );
    }

    private ExpressionSyntax CreateActionOrFuncExpression(
        ExpressionSyntax methodGroupExpression,
        SyntaxSerializationContext context )
    {
        // Check that the method doesn't have ref/out/in parameters, which can't be represented by Action<>/Func<>.
        foreach ( var param in this._method.Parameters )
        {
            if ( param.RefKind is RefKind.Ref or RefKind.Out or RefKind.In or RefKind.RefReadOnly )
            {
                throw new InvalidOperationException(
                    $"Cannot create a delegate expression for the overloaded method '{this._method}' because it has a '{param.RefKind}' parameter '{param.Name}'. " +
                    $"Action<> and Func<> delegates cannot represent ref/out/in parameters, and a typed delegate is needed to disambiguate overloads. " +
                    $"Assign the expression to a variable of a specific delegate type to disambiguate." );
            }
        }

        var isVoid = this._method.ReturnType.SpecialType == SpecialType.Void;
        var parameterTypes = this._method.Parameters.SelectAsImmutableArray( p => context.SyntaxGenerator.TypeSyntax( p.Type ) );

        TypeSyntax delegateTypeSyntax;

        if ( isVoid )
        {
            if ( parameterTypes.Length == 0 )
            {
                // Action (non-generic)
                delegateTypeSyntax = QualifiedName(
                    AliasQualifiedName(
                        SyntaxFactoryEx.WellKnownIdentifierName( Token( SyntaxKind.GlobalKeyword ) ),
                        SyntaxFactoryEx.WellKnownIdentifierName( "System" ) ),
                    SyntaxFactoryEx.WellKnownIdentifierName( "Action" ) );
            }
            else
            {
                // Action<T1, T2, ...>
                delegateTypeSyntax = QualifiedName(
                    AliasQualifiedName(
                        SyntaxFactoryEx.WellKnownIdentifierName( Token( SyntaxKind.GlobalKeyword ) ),
                        SyntaxFactoryEx.WellKnownIdentifierName( "System" ) ),
                    GenericName(
                        SyntaxFactoryEx.WellKnownIdentifier( "Action" ),
                        TypeArgumentList( SeparatedList<TypeSyntax>( parameterTypes ) ) ) );
            }
        }
        else
        {
            // Func<T1, T2, ..., TReturn>
            var allTypeArgs = parameterTypes.Add( context.SyntaxGenerator.TypeSyntax( this._method.ReturnType ) );

            delegateTypeSyntax = QualifiedName(
                AliasQualifiedName(
                    SyntaxFactoryEx.WellKnownIdentifierName( Token( SyntaxKind.GlobalKeyword ) ),
                    SyntaxFactoryEx.WellKnownIdentifierName( "System" ) ),
                GenericName(
                    SyntaxFactoryEx.WellKnownIdentifier( "Func" ),
                    TypeArgumentList( SeparatedList<TypeSyntax>( allTypeArgs ) ) ) );
        }

        // Generate: new DelegateType(methodGroupExpression)
        return ObjectCreationExpression( delegateTypeSyntax )
            .WithArgumentList(
                ArgumentList(
                    SingletonSeparatedList(
                        Argument( methodGroupExpression ) ) ) );
    }
}
