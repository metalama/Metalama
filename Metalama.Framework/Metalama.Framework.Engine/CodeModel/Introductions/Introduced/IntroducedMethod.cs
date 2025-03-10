// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Invokers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

internal sealed class IntroducedMethod : IntroducedMember, IMethodImpl
{
    private readonly MethodBuilderData _methodBuilderData;

    public IntroducedMethod( MethodBuilderData builderData, CompilationModel compilation, IGenericContext genericContext ) : base( compilation, genericContext )
    {
        this._methodBuilderData = builderData;
    }

    public override DeclarationBuilderData BuilderData => this._methodBuilderData;

    protected override NamedDeclarationBuilderData NamedDeclarationBuilderData => this._methodBuilderData;

    protected override MemberOrNamedTypeBuilderData MemberOrNamedTypeBuilderData => this._methodBuilderData;

    protected override MemberBuilderData MemberBuilderData => this._methodBuilderData;

    public override bool IsExplicitInterfaceImplementation => this.ExplicitInterfaceImplementations.Count > 0;

    [Memo]
    public IParameterList Parameters
        => new ParameterList(
            this,
            this.Compilation.GetParameterCollection( this._methodBuilderData.ToRef() ) );

    public MethodKind MethodKind => this._methodBuilderData.MethodKind;

    public OperatorKind OperatorKind => this._methodBuilderData.OperatorKind;

    public bool IsReadOnly => this._methodBuilderData.IsReadOnly;

    // TODO: When an interface is introduced, explicit implementation should appear here.
    [Memo]
    public IReadOnlyList<IMethod> ExplicitInterfaceImplementations
        => this._methodBuilderData.ExplicitInterfaceImplementations.SelectAsImmutableArray( this.MapDeclaration );

    public MethodInfo ToMethodInfo() => CompileTimeMethodInfo.Create( this );

    IHasAccessors? IMethod.DeclaringMember => null;

    [Memo]
    private IFullRef<IMethod> Ref => this.RefFactory.FromIntroducedDeclaration<IMethod>( this );

    public MethodBase ToMethodBase() => throw new NotImplementedException();

    IRef<IMethodBase> IMethodBase.ToRef() => this.ToRef();

    public IMethod MakeGenericInstance( IReadOnlyList<IType> typeArguments ) => throw new NotImplementedException();

    protected override IFullRef<IMember> ToMemberFullRef() => this.Ref;

    public IRef<IMethod> ToRef() => this.Ref;

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    [Memo]
    public IParameter ReturnParameter => new IntroducedParameter( this._methodBuilderData.ReturnParameter, this.Compilation, this.GenericContext, this );

    [Memo]
    public IType ReturnType => this.MapType( this._methodBuilderData.ReturnParameter.Type );

    [Memo]
    public ITypeParameterList TypeParameters
        => new TypeParameterList(
            this,
            this._methodBuilderData.TypeParameters.Select( x => this.RefFactory.FromBuilderData<ITypeParameter>( x ) ).ToReadOnlyList() );

    public IReadOnlyList<IType> TypeArguments => this.TypeParameters;

    public bool IsGeneric => !this._methodBuilderData.TypeParameters.IsEmpty;

    public bool IsCanonicalGenericInstance => throw new NotImplementedException();
    
    [Memo]
    public IMethod? OverriddenMethod => this.MapDeclaration( this._methodBuilderData.OverriddenMethod );

    [Memo]
    public IMethod Definition => this.Compilation.Factory.GetMethod( this._methodBuilderData ).AssertNotNull();

    protected override IMemberOrNamedType GetDefinition() => this.Definition;

    public IMethodInvoker With( InvokerOptions options ) => new MethodInvoker( this, options );

    public IMethodInvoker With( object? target, InvokerOptions options = default ) => new MethodInvoker( this, options, target );

    public IMethodInvoker With( IExpression target, InvokerOptions options = default ) => new MethodInvoker( this, options, target );

    public IExpression CreateInvokeExpression( IEnumerable<IExpression> args ) => new MethodInvoker( this ).CreateInvokeExpression( args );

    public object? Invoke( params object?[] args ) => new MethodInvoker( this ).Invoke( args );

    public object? Invoke( IEnumerable<IExpression> args ) => new MethodInvoker( this ).Invoke( args );

    public bool? IsIteratorMethod => this._methodBuilderData.IsIteratorMethod;
}