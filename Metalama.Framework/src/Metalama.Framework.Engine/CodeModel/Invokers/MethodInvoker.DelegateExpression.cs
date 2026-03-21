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

internal sealed partial class MethodInvoker
{
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

        /// <summary>
        /// Returns the actual type of the syntax produced by <see cref="ToSyntax"/> for the given <paramref name="targetType"/>.
        /// This override is necessary because <see cref="ToSyntax"/> may produce syntax whose type differs from <see cref="Type"/>
        /// (the default delegate type): when the target type is a compatible delegate, the output is either a target-typed
        /// method group or a <c>new TargetDelegateType(methodGroup)</c> expression, both of which have the target type.
        /// </summary>
        protected override IType GetSyntaxType( IType? targetType )
        {
            if ( this._explicitDelegateType != null )
            {
                if ( targetType is INamedType { TypeKind: TypeKind.Delegate } targetDelegateType
                     && IsExactMatchWithDelegate( this.Method, targetDelegateType ) )
                {
                    // Bare method group, target-typed by the compiler.
                    return targetType;
                }

                // new ExplicitDelegateType(methodGroup).
                return this._explicitDelegateType;
            }

            if ( !this._hasOverloads )
            {
                // Bare method group with a natural type (C# 10+).
                return this.Type;
            }

            if ( targetType is INamedType { TypeKind: TypeKind.Delegate } compatibleTargetDelegateType
                 && IsCompatibleWithDelegate( this.Method, compatibleTargetDelegateType ) )
            {
                // Either a bare method group (exact match, target-typed) or new TargetDelegateType(methodGroup).
                // In both cases the expression type matches the target type.
                return targetType;
            }

            // new Action<>/Func<>(methodGroup).
            return this.Type;
        }

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
                // No overloads: a bare method group is sufficient when the target type is a concrete delegate type
                // (compiler handles the conversion) or when there is no target type (C# 10+ natural function type).
                // However, method groups cannot be implicitly converted to non-delegate types (e.g. object,
                // System.Delegate), so we emit an explicit delegate creation in those cases.
                if ( targetType != null
                     && targetType is not INamedType { TypeKind: TypeKind.Delegate }
                     && this.Type is INamedType { TypeKind: TypeKind.Delegate } defaultDelegate )
                {
                    return CreateDelegateCreationExpression( methodGroupExpression, defaultDelegate, syntaxSerializationContext );
                }

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
        /// <remarks>
        /// Parameter compatibility is checked in the correct direction for delegate conversion:
        /// <b>delegate</b> parameter types must be convertible to <b>method</b> parameter types (contravariance),
        /// not the other way around. For by-ref parameters, types must match exactly.
        /// </remarks>
        private static bool IsCompatibleWithDelegate( IMethod method, INamedType delegateType )
        {
            // Get the delegate's Invoke method.
            var invokeMethod = delegateType.Methods.OfName( "Invoke" ).SingleOrDefault();

            if ( invokeMethod == null )
            {
                return false;
            }

            var methodParameters = method.Parameters;
            var delegateParameters = invokeMethod.Parameters;

            if ( methodParameters.Count != delegateParameters.Count )
            {
                return false;
            }

            // Check parameter compatibility.
            for ( var i = 0; i < methodParameters.Count; i++ )
            {
                var methodParam = methodParameters[i];
                var delegateParam = delegateParameters[i];

                // RefKind must match exactly.
                if ( methodParam.RefKind != delegateParam.RefKind )
                {
                    return false;
                }

                if ( methodParam.RefKind != RefKind.None )
                {
                    // For by-ref parameters, types must be exactly equal.
                    if ( !methodParam.Type.Equals( delegateParam.Type ) )
                    {
                        return false;
                    }
                }
                else
                {
                    // Contravariant parameter compatibility:
                    // delegate parameter type must be convertible to method parameter type.
                    if ( !delegateParam.Type.IsConvertibleTo( methodParam.Type ) )
                    {
                        return false;
                    }
                }
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
                            Argument( methodGroupExpression ) ) ) )
                .WithSimplifierAnnotationIfNecessary( context.SyntaxGenerationContext );
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
                            Argument( methodGroupExpression ) ) ) )
                .WithSimplifierAnnotationIfNecessary( context.SyntaxGenerationContext );
        }
    }
}
