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
using Metalama.Framework.Engine.CodeModel.Invokers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.ReflectionMocks;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal sealed class MethodBuilder : MethodBaseBuilder, IMethodBuilderImpl
{
    private bool _isReadOnly;
    private bool _isIteratorMethod;

    public IntroducedRef<IMethod> Ref { get; }

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

        this.Ref = new IntroducedRef<IMethod>( this.Compilation.RefFactory );
        this.DeclarationKind = declarationKind;
        this.OperatorKind = operatorKind;

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
        => this.DeclarationKind switch
        {
            DeclarationKind.Method => MethodKind.Default,
            DeclarationKind.Operator => MethodKind.Operator,
            DeclarationKind.Finalizer => MethodKind.Finalizer,
            _ => throw new AssertionFailedException( $"Unexpected DeclarationKind: {this.DeclarationKind}." )
        };

    public override MethodBase ToMethodBase() => this.ToMethodInfo();

    public override DeclarationKind DeclarationKind { get; }

    public OperatorKind OperatorKind { get; }

    IMethod IMethod.Definition => this;

    public IMethodInvoker With( InvokerOptions options ) => new MethodInvoker( this, options );

    public IMethodInvoker With( object? target, InvokerOptions options = default ) => new MethodInvoker( this, options, target );

    public IMethodInvoker With( IExpression target, InvokerOptions options = default ) => new MethodInvoker( this, options, target );

    public IExpression CreateInvokeExpression( IEnumerable<IExpression> args ) => new MethodInvoker( this ).CreateInvokeExpression( args );

    public object? Invoke( params object?[] args ) => new MethodInvoker( this ).Invoke( args );

    public object? Invoke( IEnumerable<IExpression> args ) => new MethodInvoker( this ).Invoke( args );

    public IReadOnlyList<IMethod> ExplicitInterfaceImplementations { get; private set; } = Array.Empty<IMethod>();

    public bool? IsIteratorMethod => this._isIteratorMethod;

    internal void SetIsIteratorMethod( bool value ) => this._isIteratorMethod = value;

    public void SetExplicitInterfaceImplementation( IMethod interfaceMethod ) => this.ExplicitInterfaceImplementations = [interfaceMethod];

    public override bool IsExplicitInterfaceImplementation => this.ExplicitInterfaceImplementations.Count > 0;

    public override IMember? OverriddenMember => (IMemberImpl?) this.OverriddenMethod;
        
    public override bool IsDesignTimeObservable => base.IsDesignTimeObservable || this.HasCovariantReturnType();

    public new IRef<IMethod> ToRef() => this.Ref;

    IMethod IMethod.MakeGenericInstance( IReadOnlyList<IType> typeArguments ) => throw new NotSupportedException();

    protected override IFullRef<IMember> ToMemberFullRef() => this.Ref;

    protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    protected override void EnsureReferenceInitialized()
    {
        this.Ref.BuilderData = new MethodBuilderData( this, this.ContainingDeclaration.ToFullRef() );
    }

    public MethodBuilderData BuilderData => (MethodBuilderData) this.Ref.BuilderData;
}