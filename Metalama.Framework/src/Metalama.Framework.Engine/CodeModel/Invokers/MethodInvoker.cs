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

internal sealed class MethodInvoker : Invoker<IMethod>, IMethodInvoker
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
        var hasOverloads = method.DeclaringType.Methods.OfName( method.Name ).Count() > 1;

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

    /// <summary>
    /// An <see cref="UserExpression"/> that represents a method referenced as a delegate.
    /// The generated syntax depends on the target type (when available), on whether the method has overloads,
    /// and on whether an explicit delegate type was specified.
    /// </summary>
    private sealed class DelegateExpression : UserExpression
    {
        private const int _maxActionFuncParameters = 16;

        private readonly MethodInvoker _invoker;
        private readonly bool _hasOverloads;
        private readonly INamedType? _explicitDelegateType;

        public DelegateExpression( MethodInvoker invoker, bool hasOverloads, IType defaultDelegateType, INamedType? explicitDelegateType )
        {
            this._invoker = invoker;
            this._hasOverloads = hasOverloads;
            this._explicitDelegateType = explicitDelegateType;
            this.Type = defaultDelegateType;
        }

        private IMethod Method => this._invoker.Member;

        public override IType Type { get; }

        protected override bool CanBeNull => false;

        protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
        {
            var methodGroupExpression = this.BuildMethodGroupExpression( syntaxSerializationContext );

            // If an explicit delegate type was specified, use it for disambiguation,
            // unless the target type is an exact match (in which case the C# compiler resolves the overload).
            if ( this._explicitDelegateType != null )
            {
                if ( targetType is INamedType { TypeKind: TypeKind.Delegate } targetDelegateType
                     && IsExactMatchWithDelegate( this.Method, targetDelegateType ) )
                {
                    return methodGroupExpression;
                }

                return CreateDelegateCreationExpression( methodGroupExpression, this._explicitDelegateType, syntaxSerializationContext );
            }

            if ( !this._hasOverloads )
            {
                // No overloads: a simple method group expression is always sufficient.
                // Metalama requires C# 10+, where method groups have a natural type when unambiguous,
                // so a bare method group expression (e.g. this.Method) is valid even in typeless contexts like var.
                return methodGroupExpression;
            }

            // There are overloads, so we need to disambiguate.
            // Try using the target type if it's a compatible delegate type.
            if ( targetType is INamedType { TypeKind: TypeKind.Delegate } compatibleTargetDelegateType
                 && IsCompatibleWithDelegate( this.Method, compatibleTargetDelegateType ) )
            {
                if ( IsExactMatchWithDelegate( this.Method, compatibleTargetDelegateType ) )
                {
                    // The target delegate type is an exact match for the method's signature.
                    // The C# compiler can resolve the overload from the target type context,
                    // so a simple method group expression is sufficient.
                    return methodGroupExpression;
                }

                // The target type is a compatible (but not exact) delegate. We need explicit disambiguation.
                // Generate: new TargetDelegateType(methodGroup)
                return CreateDelegateCreationExpression( methodGroupExpression, compatibleTargetDelegateType, syntaxSerializationContext );
            }

            // No usable target type. Fall back to Action<>/Func<>.
            return this.CreateActionOrFuncExpression( methodGroupExpression, syntaxSerializationContext );
        }

        private ExpressionSyntax BuildMethodGroupExpression( SyntaxSerializationContext context )
        {
            SimpleNameSyntax name;

            if ( this.Method.IsGeneric )
            {
                name = GenericName(
                    SyntaxFactoryEx.SafeIdentifier( this._invoker.GetCleanTargetMemberName() ),
                    TypeArgumentList(
                        SeparatedList( this.Method.TypeArguments.SelectAsImmutableArray( t => context.SyntaxGenerator.TypeSyntax( t ) ) ) ) );
            }
            else
            {
                name = SyntaxFactoryEx.SafeIdentifierName( this._invoker.GetCleanTargetMemberName() );
            }

            var receiverInfo = this._invoker.GetReceiverInfo( context );
            var receiverSyntax = receiverInfo.GetReceiverSyntax( this.Method, context );

            ExpressionSyntax methodGroupExpression =
                MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, receiverSyntax, name )
                    .WithSimplifierAnnotationIfNecessary( context.SyntaxGenerationContext );

            // Add aspect reference annotation if applicable.
            if ( GetTargetType()?.IsConvertibleTo( this.Method.DeclaringType ) ?? false )
            {
                methodGroupExpression = methodGroupExpression.WithAspectReferenceAnnotation(
                    receiverInfo.WithSyntax( receiverSyntax ).AspectReferenceSpecification.WithTargetKind( AspectReferenceTargetKind.Self ) );
            }

            return methodGroupExpression;
        }

        /// <summary>
        /// Checks if a method's signature is compatible with a delegate type, honoring C# method-group-to-delegate
        /// conversion rules (contravariant parameters, covariant return type, and exact match for by-ref parameters).
        /// </summary>
        private static bool IsCompatibleWithDelegate( IMethod method, INamedType delegateType )
        {
            var parameterTypes = method.Parameters.SelectAsImmutableArray( p => p.Type );
            var refKinds = method.Parameters.SelectAsImmutableArray( p => p.RefKind );

            // Use ConversionKind.Default for delegate compatibility: this allows implicit reference conversions
            // (contravariance for parameters), while by-ref parameters still require exact type match
            // because SignatureMatcher checks RefKind equality.
            var invokeMethod = delegateType.Methods.OfCompatibleSignature( "Invoke", parameterTypes, refKinds, isStatic: false, ConversionKind.Default );

            if ( invokeMethod == null )
            {
                return false;
            }

            // Check return type compatibility (covariance).
            var methodReturnType = method.ReturnType;
            var delegateReturnType = invokeMethod.ReturnType;

            var isVoid = methodReturnType.SpecialType == SpecialType.Void;
            var delegateIsVoid = delegateReturnType.SpecialType == SpecialType.Void;

            if ( isVoid || delegateIsVoid )
            {
                return isVoid && delegateIsVoid;
            }

            // For non-void return types, the method's return type must be convertible to the delegate's return type.
            return methodReturnType.IsConvertibleTo( delegateReturnType );
        }

        /// <summary>
        /// Checks if a method's signature is an exact (identical) match with a delegate type.
        /// When this is the case, the C# compiler can resolve the overload from the target type context
        /// without explicit disambiguation (i.e., a bare method group is sufficient).
        /// </summary>
        private static bool IsExactMatchWithDelegate( IMethod method, INamedType delegateType )
        {
            var parameterTypes = method.Parameters.SelectAsImmutableArray( p => p.Type );
            var refKinds = method.Parameters.SelectAsImmutableArray( p => p.RefKind );

            // Use ConversionKind.Identical to require exact type equality for parameters.
            var invokeMethod = delegateType.Methods.OfCompatibleSignature( "Invoke", parameterTypes, refKinds, isStatic: false, ConversionKind.Identical );

            if ( invokeMethod == null )
            {
                return false;
            }

            // Check return type: must be exactly identical.
            var methodReturnType = method.ReturnType;
            var delegateReturnType = invokeMethod.ReturnType;

            return methodReturnType.Equals( delegateReturnType );
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
            foreach ( var param in this.Method.Parameters )
            {
                if ( param.RefKind is RefKind.Ref or RefKind.Out or RefKind.In or RefKind.RefReadOnly )
                {
                    throw new InvalidOperationException(
                        $"Cannot create a delegate expression for the overloaded method '{this.Method}' because it has a '{param.RefKind}' parameter '{param.Name}'. " +
                        $"Action<> and Func<> delegates cannot represent ref/out/in parameters, and a typed delegate is needed to disambiguate overloads. " +
                        $"Use the 'delegateType' parameter of CreateDelegateExpression or assign the expression to a variable of a specific delegate type." );
                }
            }

            // Check that the parameter count doesn't exceed the Action<>/Func<> limit.
            if ( this.Method.Parameters.Count > _maxActionFuncParameters )
            {
                throw new InvalidOperationException(
                    $"Cannot create a delegate expression for the method '{this.Method}' because it has {this.Method.Parameters.Count} parameters, " +
                    $"which exceeds the maximum of {_maxActionFuncParameters} supported by Action<> and Func<>. " +
                    $"Use the 'delegateType' parameter of CreateDelegateExpression to specify a custom delegate type." );
            }

            var isVoid = this.Method.ReturnType.SpecialType == SpecialType.Void;
            var parameterTypes = this.Method.Parameters.SelectAsImmutableArray( p => context.SyntaxGenerator.TypeSyntax( p.Type ) );

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
                var allTypeArgs = parameterTypes.Add( context.SyntaxGenerator.TypeSyntax( this.Method.ReturnType ) );

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
}