// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using MethodKind = Metalama.Framework.Code.MethodKind;

namespace Metalama.Framework.Engine.CodeModel.Invokers;

internal sealed partial class MethodInvoker : Invoker<IMethod>, IMethodInvoker
{
    private readonly bool _skipTypeArgumentInference;

    public MethodInvoker( IMethod method, InvokerOptions options = default, IExpression? target = null ) : base( method, options, target ) { }

    private MethodInvoker( IMethod method, InvokerOptions options, IExpression? target, bool skipTypeArgumentInference )
        : base( method, options, target )
    {
        this._skipTypeArgumentInference = skipTypeArgumentInference;
    }

    public object? Invoke( IEnumerable<IExpression> args ) => this.InvokeCore( args.ToReadOnlyList() );

    private object? InvokeCore( IReadOnlyList<IExpression> args )
    {
        var parametersCount = this.Member.Parameters.Count;

        if ( parametersCount > 0 && this.Member.Parameters[parametersCount - 1].IsParams )
        {
            // The this.Declaration has a 'params' param.
            if ( args.Count < parametersCount - 1 )
            {
                throw GeneralDiagnosticDescriptors.MemberRequiresAtLeastNArguments.CreateException( (this.Member, parametersCount - 1, args.Count) );
            }
        }
        else if ( args.Count != parametersCount )
        {
            throw GeneralDiagnosticDescriptors.MemberRequiresNArguments.CreateException( (this.Member, parametersCount, args.Count) );
        }

        // Detect the common mistake of passing an array as a single argument
        // (e.g., method.Invoke(new object[] { 1 })) when the target parameter is not an array type.
        for ( var i = 0; i < args.Count && i < parametersCount; i++ )
        {
            var argType = args[i].Type;
            var paramType = this.Member.Parameters[i].Type;

            if ( argType.TypeKind == TypeKind.Array && paramType.TypeKind != TypeKind.Array && !this.Member.Parameters[i].IsParams )
            {
                throw GeneralDiagnosticDescriptors.ArrayPassedAsSingleArgument.CreateException(
                    (this.Member, argType.ToString() ?? "array", this.Member.Parameters[i].Name, paramType.ToString() ?? "unknown") );
            }
        }

        this.CheckInvocationOptionsAndTarget();

        switch ( this.Member.MethodKind )
        {
            case MethodKind.Default:
            case MethodKind.LocalFunction:
            case MethodKind.ExplicitInterfaceImplementation:
                return this.InvokeDefaultMethod( args );

            case MethodKind.EventAdd:
                return ((IEvent) this.Member.DeclaringMember!).WithObject( this.Target ).WithOptions( this.Options ).Add( args[0] );

            case MethodKind.EventRaise:
                return ((IEvent) this.Member.DeclaringMember!).WithObject( this.Target ).WithOptions( this.Options ).Raise( args );

            case MethodKind.EventRemove:
                return ((IEvent) this.Member.DeclaringMember!).WithObject( this.Target ).WithOptions( this.Options ).Remove( args[0] );

            case MethodKind.PropertyGet:
                switch ( this.Member.DeclaringMember!.DeclarationKind )
                {
                    case DeclarationKind.Property when this.Member.DeclaringMember is IProperty property:
                        return property.WithObject( this.Target ).WithOptions( this.Options ).Value;

                    case DeclarationKind.Indexer when this.Member.DeclaringMember is IIndexer indexer:
                        return indexer.WithObject( this.Target! ).WithOptions( this.Options )[args];

                    default:
                        throw new AssertionFailedException( $"Unexpected declaration for a PropertyGet: '{this.Member.DeclaringMember}'." );
                }

            case MethodKind.PropertySet:
                switch ( this.Member.DeclaringMember!.DeclarationKind )
                {
                    case DeclarationKind.Property when this.Member.DeclaringMember is IProperty property:
                        ((FieldOrPropertyInvoker) property.WithObject( this.Target ).WithOptions( this.Options )).SetValue( args[0] );

                        return null;

                    case DeclarationKind.Indexer when this.Member.DeclaringMember is IIndexer indexer:
                        ((IndexerInvoker) indexer.WithObject( this.Target! ).WithOptions( this.Options )).SetValue( this.Target, args );

                        return null;

                    default:
                        throw new AssertionFailedException( $"Unexpected declaration for a PropertySet: '{this.Member.DeclaringMember}'." );
                }

            default:
                throw new NotImplementedException(
                    $"Cannot generate syntax to invoke the this.Declaration '{this.Member}' because this.Declaration kind {this.Member.MethodKind} is not implemented." );
        }
    }

    public object? Invoke( IEnumerable<object?> args ) => this.Invoke( args.ToArray() );

    public object? Invoke( object?[]? args )
    {
        // For some reason, overload resolution chooses the wrong overload in the template,
        // so redirect to the correct one.
        if ( args is [IEnumerable<IExpression> expressionArgs] )
        {
            return this.InvokeCore( expressionArgs.ToReadOnlyList() );
        }

        return this.InvokeCore( CapturedUserExpression.Create( this.Compilation, args ) );
    }

    private DelegateUserExpression InvokeDefaultMethod( IReadOnlyList<IExpression> args )
    {
        // For extension members, redirect to the implementation method.
        if ( this.IsExtensionMember )
        {
            return this.InvokeExtensionImplementationMethod( args );
        }

        // If the method is a canonical generic instance (type arguments are the type parameters themselves),
        // attempt to infer type arguments from the actual argument types. This prevents generating invalid code
        // with unresolved type parameters like `Bar<T>` instead of `Bar<int>`. (See #765)
        var resolvedMethod = this.Member;

        if ( resolvedMethod.IsGeneric && resolvedMethod.IsCanonicalGenericInstance && args.Count > 0
             && !this._skipTypeArgumentInference )
        {
            var inferredMethod = TryInferTypeArguments( resolvedMethod, args );

            if ( inferredMethod != null )
            {
                resolvedMethod = inferredMethod;
            }
            else
            {
                throw GeneralDiagnosticDescriptors.CannotInferTypeArguments.CreateException( resolvedMethod );
            }
        }

        var methodForCodeGen = resolvedMethod;

        return new DelegateUserExpression(
            context =>
            {
                SimpleNameSyntax name;

                var receiverInfo = this.GetReceiverInfo( context );

                if ( methodForCodeGen.IsGeneric )
                {
                    name = GenericName(
                        SyntaxFactoryEx.SafeIdentifier( this.GetCleanTargetMemberName() ),
                        TypeArgumentList( SeparatedList( methodForCodeGen.TypeArguments.SelectAsImmutableArray( t => context.SyntaxGenerator.TypeSyntax( t ) ) ) ) );
                }
                else
                {
                    name = SyntaxFactoryEx.SafeIdentifierName( this.GetCleanTargetMemberName() );
                }

                var arguments = methodForCodeGen.GetArguments( methodForCodeGen.Parameters, args, context );

                if ( this.Member.MethodKind == MethodKind.LocalFunction )
                {
                    if ( receiverInfo.Syntax.Kind() != SyntaxKind.NullLiteralExpression )
                    {
                        throw GeneralDiagnosticDescriptors.CannotProvideInstanceForLocalFunction.CreateException( this.Member );
                    }

                    return this.CreateInvocationExpression(
                        receiverInfo.ToReceiverExpressionSyntax(),
                        name,
                        arguments,
                        AspectReferenceTargetKind.Self,
                        context );
                }
                else
                {
                    var receiverSyntax = receiverInfo.GetReceiverSyntax( this.Member, context );
                    var receiver = receiverInfo.WithSyntax( receiverSyntax );

                    return this.CreateInvocationExpression( receiver, name, arguments, AspectReferenceTargetKind.Self, context );
                }
            },
            (this.Options & InvokerOptions.NullConditional) != 0 ? methodForCodeGen.ReturnType.ToNullable() : methodForCodeGen.ReturnType );
    }

    /// <summary>
    /// Attempts to infer type arguments for a canonical generic method from the actual argument types.
    /// Returns a constructed generic method if all type parameters can be inferred, or <c>null</c> otherwise.
    /// </summary>
    private static IMethod? TryInferTypeArguments( IMethod method, IReadOnlyList<IExpression> args )
    {
        var typeParameters = method.TypeParameters;
        var inferredTypes = new IType?[typeParameters.Count];

        // Match each argument type against the corresponding parameter type to infer type arguments.
        var parameters = method.Parameters;

        for ( var i = 0; i < Math.Min( args.Count, parameters.Count ); i++ )
        {
            var argType = args[i].Type;
            var paramType = parameters[i].Type;

            TryMatchTypeArguments( paramType, argType, typeParameters, inferredTypes );
        }

        // Check if all type parameters were inferred.
        for ( var i = 0; i < inferredTypes.Length; i++ )
        {
            if ( inferredTypes[i] == null )
            {
                return null;
            }
        }

        return method.MakeGenericInstance( inferredTypes! );
    }

    /// <summary>
    /// Recursively matches an argument type against a parameter type to discover type parameter mappings.
    /// For example, matching <c>TestData&lt;int&gt;</c> against <c>TestData&lt;T&gt;</c> infers <c>T = int</c>.
    /// </summary>
    private static void TryMatchTypeArguments(
        IType parameterType,
        IType argumentType,
        IReadOnlyList<ITypeParameter> typeParameters,
        IType?[] inferredTypes )
    {
        if ( parameterType.TypeKind == TypeKind.TypeParameter && parameterType is ITypeParameter typeParam )
        {
            // Check that this type parameter belongs to the method (not the declaring type).
            if ( typeParam.TypeParameterKind == TypeParameterKind.Method && typeParam.Index < inferredTypes.Length )
            {
                var existingType = inferredTypes[typeParam.Index];

                if ( existingType == null )
                {
                    inferredTypes[typeParam.Index] = argumentType;
                }
                else if ( !existingType.Equals( argumentType ) )
                {
                    // Conflicting inference — mark as failed by using a sentinel.
                    // TryInferTypeArguments checks for null, so we need a way to signal conflict.
                    // We set to null and rely on the null check in TryInferTypeArguments to fail.
                    inferredTypes[typeParam.Index] = null;
                }
            }

            return;
        }

        // For named types (e.g., TestData<T>), match type arguments recursively.
        if ( parameterType is INamedType paramNamedType && argumentType is INamedType argNamedType )
        {
            if ( paramNamedType.TypeArguments.Count > 0 &&
                 paramNamedType.TypeArguments.Count == argNamedType.TypeArguments.Count &&
                 paramNamedType.Definition.Equals( argNamedType.Definition ) )
            {
                for ( var i = 0; i < paramNamedType.TypeArguments.Count; i++ )
                {
                    TryMatchTypeArguments( paramNamedType.TypeArguments[i], argNamedType.TypeArguments[i], typeParameters, inferredTypes );
                }
            }
        }

        // For array types, match element types only when ranks are equal.
        if ( parameterType is IArrayType paramArrayType && argumentType is IArrayType argArrayType
             && paramArrayType.Rank == argArrayType.Rank )
        {
            TryMatchTypeArguments( paramArrayType.ElementType, argArrayType.ElementType, typeParameters, inferredTypes );
        }
    }

    private DelegateUserExpression InvokeExtensionImplementationMethod( IReadOnlyList<IExpression> args )
    {
        var implMethod = this.Member.ExtensionImplementationMethod;

        if ( implMethod == null )
        {
            throw new InvalidOperationException( $"Cannot invoke extension member '{this.Member}' because its implementation method was not found." );
        }

        // The implementation method is always static, so we pass null as target.
        // Skip type argument inference for extension implementation methods because
        // their type parameters (from the extension block) should be kept as-is.
        var implInvoker = new MethodInvoker( implMethod, this.Options, target: null, skipTypeArgumentInference: true );

        // For instance extension members, the receiver becomes the first argument.
        IReadOnlyList<IExpression> implArgs;

        if ( this.Member.IsStatic )
        {
            implArgs = args;
        }
        else
        {
            if ( this.Target == null )
            {
                throw new InvalidOperationException(
                    $"Cannot invoke instance extension member '{this.Member}' because the receiver (target) expression is null." );
            }

            var combinedArgs = new List<IExpression>( 1 + args.Count ) { this.Target };
            combinedArgs.AddRange( args );
            implArgs = combinedArgs;
        }

        return (DelegateUserExpression) implInvoker.CreateInvokeExpression( implArgs );
    }

    private ExpressionSyntax CreateInvocationExpression(
        ReceiverExpressionSyntax receiverTypedExpressionSyntax,
        SimpleNameSyntax name,
        ArgumentSyntax[]? arguments,
        AspectReferenceTargetKind targetKind,
        SyntaxSerializationContext context )
    {
        if ( !receiverTypedExpressionSyntax.RequiresNullConditionalAccessMember )
        {
            ExpressionSyntax memberAccessExpression =
                MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, receiverTypedExpressionSyntax.Syntax, name )
                    .WithSimplifierAnnotationIfNecessary( context.SyntaxGenerationContext );

            // Only create an aspect reference when the declaring type of the invoked declaration is ancestor of the target of the template (or its declaring type).
            if ( GetTargetType()?.IsConvertibleTo( this.Member.DeclaringType ) ?? false )
            {
                memberAccessExpression =
                    memberAccessExpression.WithAspectReferenceAnnotation(
                        receiverTypedExpressionSyntax.AspectReferenceSpecification.WithTargetKind( targetKind ) );
            }

            return
                arguments != null
                    ? InvocationExpression(
                        memberAccessExpression,
                        ArgumentList( SeparatedList( arguments ) ) )
                    : InvocationExpression( memberAccessExpression );
        }
        else
        {
            var expression =
                arguments != null
                    ? ConditionalAccessExpression(
                        receiverTypedExpressionSyntax.Syntax,
                        InvocationExpression(
                            MemberBindingExpression( name ),
                            ArgumentList( SeparatedList( arguments ) ) ) )
                    : ConditionalAccessExpression(
                        receiverTypedExpressionSyntax.Syntax,
                        InvocationExpression( MemberBindingExpression( name ) ) );

            // Only create an aspect reference when the declaring type of the invoked declaration is ancestor of the target of the template (or its declaring type).
            if ( GetTargetType()?.IsConvertibleTo( this.Member.DeclaringType ) ?? false )
            {
                expression = expression.WithAspectReferenceAnnotation(
                    receiverTypedExpressionSyntax.AspectReferenceSpecification.WithTargetKind( targetKind ) );
            }

            return expression;
        }
    }

    public IMethodInvoker WithOptions( InvokerOptions options ) => this.Options == options ? this : new MethodInvoker( this.Member, options, this.Target );

    public IMethodInvoker WithObject( object? obj )
        => this.IsSameTarget( obj ) ? this : this.WithObject( CapturedUserExpression.Create( this.Compilation, obj ) );

    public IMethodInvoker WithObject( IExpression? obj ) => this.IsSameTarget( obj ) ? this : new MethodInvoker( this.Member, this.Options, obj );

    IMethodInvoker IMethodInvoker.With( InvokerOptions options ) => this.WithOptions( options );

    IMethodInvoker IMethodInvoker.With( object? obj, InvokerOptions options ) => this.WithOptions( options ).WithObject( obj );

    public IExpression CreateInvokeExpression( IEnumerable<IExpression> args ) => this.InvokeDefaultMethod( args.ToArray() );

    public IExpression CreateInvokeExpression( params IEnumerable<object?> args )
        => this.CreateInvokeExpression( args.Select( a => CapturedUserExpression.Create( this.Compilation, a ) ) );

    public IExpression CreateDelegateExpression( INamedType? delegateType = null )
    {
        var method = this.Member;

        // Check if the method has overloads in the declaring type.
        var hasOverloads = method.DeclaringType.Methods.OfName( method.Name ).Skip( 1 ).Any();

        // Compute the default delegate type for the IExpression.Type property.
        // If an explicit delegate type is provided, use it. Otherwise, compute Action<>/Func<>.
        var defaultDelegateType = delegateType ?? GetDefaultDelegateType( method, this.Compilation.Factory );

        return new DelegateExpression( this, hasOverloads, defaultDelegateType, delegateType );
    }

    private static IType GetDefaultDelegateType( IMethod method, IDeclarationFactory factory )
    {
        // If any parameter has a ref kind, Action<>/Func<> cannot represent it.
        // Fall back to System.Delegate.
        if ( method.Parameters.Any( p => p.RefKind is RefKind.Ref or RefKind.Out or RefKind.In or RefKind.RefReadOnly ) )
        {
            return factory.GetTypeByReflectionType( typeof(Delegate) );
        }

        // Action<> supports up to 16 parameters; Func<> supports up to 16 input parameters + 1 return.
        if ( method.Parameters.Count > 16 )
        {
            return factory.GetTypeByReflectionType( typeof(Delegate) );
        }

        var isVoid = method.ReturnType.SpecialType == SpecialType.Void;

        if ( isVoid )
        {
            if ( method.Parameters.Count == 0 )
            {
                return factory.GetTypeByReflectionType( typeof(Action) );
            }
            else
            {
                var genericAction = factory.GetTypeByReflectionType( Type.GetType( "System.Action`" + method.Parameters.Count )! );

                return ((INamedType) genericAction).MakeGenericInstance( method.Parameters.SelectAsArray( p => p.Type ) );
            }
        }
        else
        {
            var genericFunc = factory.GetTypeByReflectionType( Type.GetType( "System.Func`" + (method.Parameters.Count + 1) )! );
            var funcTypeArgs = method.Parameters.SelectAsArray( p => p.Type ).Append( method.ReturnType ).ToArray();

            return ((INamedType) genericFunc).MakeGenericInstance( funcTypeArgs );
        }
    }

}