// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.Collections;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using System;
using System.Collections.Generic;
using System.Reflection;
using MethodInvoker = Metalama.Framework.Engine.CodeModel.Invokers.MethodInvoker;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal sealed class MethodBuilder : MethodBaseBuilder, IMethodBuilderImpl
{
    private bool _isReadOnly;
    private bool _isIteratorMethod;
    private bool _isImplicitlyDeclared;
    private DeclarationKind _declarationKind;
    private OperatorKind _operatorKind;

    public IntroducedRef<IMethod> Ref { get; }

    public override bool IsImplicitlyDeclared => this._isImplicitlyDeclared;

    public void SetImplicitlyDeclared()
    {
        this.CheckNotFrozen();
        this._isImplicitlyDeclared = true;
    }

#pragma warning disable CS0618 // DeclarationKind.Operator and DeclarationKind.Finalizer are obsolete - used internally
    public MethodBuilder(
        AspectLayerInstance aspectLayerInstance,
        INamedType declaringType,
        string name,
        DeclarationKind declarationKind = DeclarationKind.Method,
        OperatorKind operatorKind = OperatorKind.None )
        : base( aspectLayerInstance, declaringType, name )
    {
        Invariant.Assert(
            declarationKind == DeclarationKind.Operator
                            ==
                            (operatorKind != OperatorKind.None) );
#pragma warning restore CS0618

        this.Ref = new IntroducedRef<IMethod>( this.Compilation.RefFactory );
        this._declarationKind = declarationKind;
        this._operatorKind = operatorKind;

        // When created with an operator kind, set IsStatic based on the operator.
        // This must be done here because the IsStatic setter will reject changes
        // when _operatorKind is already set.
        if ( operatorKind != OperatorKind.None )
        {
            var operatorData = OperatorData.GetByKind( operatorKind );
            base.IsStatic = operatorData.IsStatic;
        }

        this.ReturnParameter =
            new ParameterBuilder(
                this,
                -1,
                null,
                this.Compilation.Cache.SystemVoidType.AssertNotNull(),
                RefKind.None,
                this.AspectLayerInstance );
    }

    public TypeParameterBuilderList TypeParameters { get; } = [];

    public override string Name
    {
        get => base.Name;
        set
        {
            if ( this._operatorKind != OperatorKind.None && value != base.Name )
            {
                throw new InvalidOperationException(
                    "Cannot change the name of an operator method. The name is automatically set based on the OperatorKind." );
            }

            base.Name = value;
        }
    }

    public override bool IsStatic
    {
        get => base.IsStatic;
        set
        {
            if ( this._operatorKind != OperatorKind.None && value != base.IsStatic )
            {
                throw new InvalidOperationException(
                    "Cannot change the IsStatic property of an operator method. It is automatically set based on the OperatorKind." );
            }

            base.IsStatic = value;
        }
    }

    public bool IsReadOnly
    {
        get => this._isReadOnly;
        set
        {
            this.CheckNotFrozen();

            this._isReadOnly = value;
        }
    }

    public IReadOnlyList<IType> TypeArguments => this.TypeParameters;

    public IMethod? OverriddenMethod { get; set; }

    public MethodInfo ToMethodInfo() => CompileTimeMethodInfo.Create( this );

    IHasAccessors? IMethod.DeclaringMember => null;

    protected override void FreezeChildren()
    {
        base.FreezeChildren();

        foreach ( var typeParameter in this.TypeParameters )
        {
            typeParameter.Freeze();
        }

        this.ReturnParameter.Freeze();
    }

    public ITypeParameterBuilder AddTypeParameter( string name )
    {
        this.CheckNotFrozen();

        var builder = new TypeParameterBuilder( this, this.TypeParameters.Count, name );
        this.TypeParameters.Add( builder );

        return builder;
    }

    /// <summary>
    /// Adds a type parameter based on a prototype type parameter. Does not copy type constraints or attributes.
    /// </summary>
    internal ITypeParameterBuilder AddTypeParameter( ITypeParameter prototype )
    {
        var typeParameterBuilder = this.AddTypeParameter( prototype.Name );

        typeParameterBuilder.Variance = prototype.Variance;
        typeParameterBuilder.HasDefaultConstructorConstraint = prototype.HasDefaultConstructorConstraint;
        typeParameterBuilder.TypeKindConstraint = prototype.TypeKindConstraint;
        typeParameterBuilder.IsConstraintNullable = prototype.IsConstraintNullable;
        typeParameterBuilder.AllowsRefStruct = prototype.AllowsRefStruct;

        return typeParameterBuilder;
    }

    IParameterBuilder IMethodBuilder.ReturnParameter => this.ReturnParameter;

    public IType ReturnType
    {
        get => this.ReturnParameter.Type;
        set
        {
            this.CheckNotFrozen();

            this.ReturnParameter.Type = value ?? throw new ArgumentNullException( nameof(value) );
        }
    }

    IType IMethod.ReturnType => this.ReturnParameter.Type;

    public BaseParameterBuilder ReturnParameter { get; }

    IParameter IMethod.ReturnParameter => this.ReturnParameter;

    IParameterList IHasParameters.Parameters => this.Parameters;

    IParameterBuilderList IHasParametersBuilder.Parameters => this.Parameters;

    ITypeParameterList IGeneric.TypeParameters => this.TypeParameters;

    public bool IsGeneric => this.TypeParameters.Count > 0;

    public bool IsCanonicalGenericInstance => true;

    // We don't currently support adding other methods than default ones.
    public MethodKind MethodKind
        => this._declarationKind switch
        {
#pragma warning disable CS0618 // DeclarationKind.Operator and DeclarationKind.Finalizer are obsolete
            DeclarationKind.Method => MethodKind.Default,
            DeclarationKind.Operator => MethodKind.Operator,
            DeclarationKind.Finalizer => MethodKind.Finalizer,
#pragma warning restore CS0618
            _ => throw new AssertionFailedException( $"Unexpected _declarationKind: {this._declarationKind}." )
        };

    public override MethodBase ToMethodBase() => this.ToMethodInfo();

    // Public API always returns Method - use MethodKind to distinguish operators/finalizers.
    public override DeclarationKind DeclarationKind => DeclarationKind.Method;

    public OperatorKind OperatorKind
    {
        get => this._operatorKind;
        set
        {
            this.CheckNotFrozen();

            if ( value == this._operatorKind )
            {
                return;
            }

            if ( value == OperatorKind.None )
            {
                // Switching from operator to regular method.
                this._operatorKind = OperatorKind.None;
                this._declarationKind = DeclarationKind.Method;

                // Name and IsStatic are not automatically reset - user must set them manually.
            }
            else
            {
                // OperatorKind can only be set once (from None to a specific value).
                if ( this._operatorKind != OperatorKind.None )
                {
                    throw new InvalidOperationException(
                        $"The OperatorKind cannot be changed from '{this._operatorKind}' to '{value}'. It can only be set once." );
                }

                // Switching to operator.
                if ( !OperatorData.IsUserDefinable( value ) )
                {
                    throw new ArgumentException(
                        $"The operator kind '{value}' is not user-definable and cannot be introduced.",
                        nameof(value) );
                }

                var operatorData = OperatorData.GetByKind( value );

                // Set Name and IsStatic BEFORE setting _operatorKind to bypass validation.
                // We use the base setters directly to avoid our validation that prevents
                // changing these properties when _operatorKind is set.
                base.Name = operatorData.MemberName;
                base.IsStatic = operatorData.IsStatic;
                this._operatorKind = value;
#pragma warning disable CS0618 // DeclarationKind.Operator is obsolete - used internally
                this._declarationKind = DeclarationKind.Operator;
#pragma warning restore CS0618
            }
        }
    }

    IMethod IMethod.Definition => this;

    [Memo]
    private IMethodInvoker Invoker => new MethodInvoker( this );

    IMethodInvoker IMethodInvoker.WithOptions( InvokerOptions options ) => this.Invoker.WithOptions( options );

    IMethodInvoker IMethodInvoker.WithObject( object? obj ) => this.Invoker.WithObject( obj );

    IMethodInvoker IMethodInvoker.WithObject( IExpression? obj ) => this.Invoker.WithObject( obj );

    IMethodInvoker IMethodInvoker.With( InvokerOptions options ) => this.Invoker.WithOptions( options );

    IMethodInvoker IMethodInvoker.With( object? obj, InvokerOptions options ) => this.Invoker.WithOptions( options ).WithObject( obj );

    IExpression IMethodInvoker.CreateInvokeExpression( IEnumerable<IExpression> args ) => this.Invoker.CreateInvokeExpression( args );

    IExpression IMethodInvoker.CreateInvokeExpression( IEnumerable<object?> args ) => this.Invoker.CreateInvokeExpression( args );

    object? IMethodInvoker.Invoke( params object?[] args ) => this.Invoker.Invoke( args );

    object? IMethodInvoker.Invoke( IEnumerable<IExpression> args ) => this.Invoker.Invoke( args );

    public IReadOnlyList<IMethod> ExplicitInterfaceImplementations { get; private set; } = Array.Empty<IMethod>();

    public bool? IsIteratorMethod => this._isIteratorMethod;

    internal void SetIsIteratorMethod( bool value ) => this._isIteratorMethod = value;

    public void SetExplicitInterfaceImplementation( IMethod interfaceMethod ) => this.ExplicitInterfaceImplementations = [interfaceMethod];

    public override bool IsExplicitInterfaceImplementation => this.ExplicitInterfaceImplementations.Count > 0;

    public override IMember? OverriddenMember => (IMemberImpl?) this.OverriddenMethod;

    public override bool IsDesignTimeObservable => base.IsDesignTimeObservable || this.HasCovariantReturnType();

    public new IRef<IMethod> ToRef() => this.Ref;

    IMethod IMethod.MakeGenericInstance( IReadOnlyList<IType> typeArguments ) => throw new NotSupportedException();

    // Builders don't have an extension implementation method; this is resolved after the transformation phase.
    IMethod? IMethod.ExtensionImplementationMethod => null;

    protected override IFullRef<IMember> ToMemberFullRef() => this.Ref;

    protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    protected override void EnsureReferenceInitialized()
    {
        this.Ref.BuilderData = new MethodBuilderData( this, this.ContainingDeclaration.ToFullRef() );
    }

    public MethodBuilderData BuilderData => (MethodBuilderData) this.Ref.BuilderData;
}