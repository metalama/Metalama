// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.ReflectionMocks;
using System;
using System.Collections.Generic;
using System.Reflection;
using MethodBase = System.Reflection.MethodBase;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal sealed class ConstructorBuilder : MethodBaseBuilder, IConstructorBuilder, IConstructorImpl
{
    private readonly bool _isImplicitlyDeclared;
    private ConstructorInitializerKind _initializerKind;
    private ConstructorBuilderData? _builderData;
    private IFullRef<IConstructor>? _ref;

    // In ConstructorBuilders, references cannot be created until freeze because it depends on the ReplacedImplicitConstructor property.
    public IFullRef<IConstructor> Ref
        => this._ref ?? throw new InvalidOperationException( "Cannot create a reference to a ConstructorBuilder until it is frozen." );

    public ConstructorBuilder( AspectLayerInstance aspectLayerInstance, INamedType declaringType, bool isImplicitlyDeclared = false )
        : base( aspectLayerInstance, declaringType, null! )
    {
        this.InitializerArguments = [];
        this._isImplicitlyDeclared = isImplicitlyDeclared;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConstructorBuilder"/> class that replaces an implicit constructor.
    /// </summary>
    public ConstructorBuilder( AspectLayerInstance aspectLayerInstance, IConstructor replacedImplicitConstructor )
        : base( aspectLayerInstance, replacedImplicitConstructor.DeclaringType, null! )
    {
        this.InitializerArguments = [];

        // We intentionally don't store a reference to the replaced constructor, but the constructor itself,
        // because references are always resolved to the _replacement_.

        Invariant.Assert( replacedImplicitConstructor is SourceConstructor or ConstructorBuilder );
        Invariant.Assert( replacedImplicitConstructor.IsImplicitlyDeclared );

        this.ReplacedImplicitConstructor = replacedImplicitConstructor;
        this.Accessibility = replacedImplicitConstructor.Accessibility;
        this.IsStatic = replacedImplicitConstructor.IsStatic;
    }

    public IConstructor? ReplacedImplicitConstructor { get; set; }

    public override bool IsImplicitlyDeclared => this._isImplicitlyDeclared;

    public ConstructorInitializerKind InitializerKind
    {
        get => this._initializerKind;
        set
        {
            this.CheckNotFrozen();
            this._initializerKind = value;
        }
    }

    public List<(IExpression Expression, string? ParameterName)> InitializerArguments { get; }

    protected override void EnsureReferenceCreated()
    {
        if ( this.ReplacedImplicitConstructor != null )
        {
            this._ref = this.ReplacedImplicitConstructor.ToFullRef();
        }
        else
        {
            this._ref = new IntroducedRef<IConstructor>( this.Compilation.RefFactory );
        }
    }

    protected override void EnsureReferenceInitialized()
    {
        this._builderData = new ConstructorBuilderData( this, this.ContainingDeclaration.ToFullRef() );

        if ( this._ref is IntroducedRef<IConstructor> introducedRef )
        {
            introducedRef.BuilderData = this._builderData;
        }
    }

    public ConstructorBuilderData BuilderData => this.AssertFrozen()._builderData!;

    public void AddInitializerArgument( IExpression expression, string? parameterName )
    {
        this.CheckNotFrozen();

        this.InitializerArguments.Add( (expression, parameterName) );
    }

    bool IConstructor.IsPrimary => false;

    public override IMember? OverriddenMember => null;

    public override bool IsExplicitInterfaceImplementation => false;

    // This is implemented by BuiltConstructor and there is no point in supporting it here.
    public IConstructor GetBaseConstructor() => throw new NotSupportedException();

    public override string Name
    {
        get => this.IsStatic ? ".cctor" : ".ctor";
        set => throw new NotSupportedException( "Setting constructor name is not supported" );
    }

    public override DeclarationKind DeclarationKind => DeclarationKind.Constructor;

    public ConstructorInfo ToConstructorInfo() => CompileTimeConstructorInfo.Create( this );

    IConstructor IConstructor.Definition => this;
    
    public override MethodBase ToMethodBase() => this.ToConstructorInfo();

    public new IRef<IConstructor> ToRef() => this.Ref;

    protected override IFullRef<IMember> ToMemberFullRef() => this.Ref;

    protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    public object Invoke( params object?[] args ) => throw new NotSupportedException( "Constructor builders cannot be invoked." );

    public object Invoke( IEnumerable<IExpression> args ) => throw new NotSupportedException( "Constructor builders cannot be invoked." );

    public IObjectCreationExpression CreateInvokeExpression() => throw new NotSupportedException( "Constructor builders cannot be invoked." );

    public IObjectCreationExpression CreateInvokeExpression( params object?[] args )
        => throw new NotSupportedException( "Constructor builders cannot be invoked." );

    public IObjectCreationExpression CreateInvokeExpression( params IExpression[] args )
        => throw new NotSupportedException( "Constructor builders cannot be invoked." );

    public IObjectCreationExpression CreateInvokeExpression( IEnumerable<IExpression> args )
        => throw new NotSupportedException( "Constructor builders cannot be invoked." );

/*
    public override ICompilationElement? Translate(
        CompilationModel newCompilation,
        IGenericContext? genericContext = null,
        Type? interfaceType = null )
        => this.ReplacedImplicitConstructor?.Translate( newCompilation ) ?? base.Translate( newCompilation, genericContext );
        */
}