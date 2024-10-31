// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Invokers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using Accessibility = Metalama.Framework.Code.Accessibility;
using MethodBase = System.Reflection.MethodBase;
using MethodKind = Metalama.Framework.Code.MethodKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

internal sealed class IntroducedAccessor : IntroducedDeclaration, IMethodImpl
{
    private readonly IntroducedMember _introducedMember;
    private readonly MethodBuilderData _builderData;

    public IntroducedAccessor( IntroducedMember introducedMember, MethodBuilderData builder ) : base(
        introducedMember.Compilation,
        introducedMember.GenericContext )
    {
        this._introducedMember = introducedMember;
        this._builderData = builder;
    }

    public override DeclarationBuilderData BuilderData => this._builderData;

    public Accessibility Accessibility => this._builderData.Accessibility;

    public string Name => this._builderData.Name;

    public bool IsPartial => this._builderData.IsPartial;

    public bool HasImplementation => !this._introducedMember.IsAbstract;

    public bool IsAbstract => this._builderData.IsAbstract;

    public bool IsStatic => this._builderData.IsStatic;

    public bool IsVirtual => this._builderData.IsVirtual;

    public bool IsSealed => this._builderData.IsSealed;

    public bool IsReadOnly => this._builderData.IsReadOnly;

    public bool IsOverride => this._builderData.IsOverride;

    public bool IsExplicitInterfaceImplementation => this.ExplicitInterfaceImplementations.Count > 0;

    public bool IsNew => this._builderData.IsNew;

    public bool? HasNewKeyword => false;

    public bool IsAsync => this._builderData.IsAsync;

    public override bool IsImplicitlyDeclared
        => this is { MethodKind: MethodKind.PropertySet, ContainingDeclaration: IProperty { Writeability: Writeability.ConstructorOnly } };

    [Memo]
    public IParameterList Parameters
        => new ParameterList(
            this,
            this.Compilation.GetParameterCollection( this.Ref ) );

    public MethodKind MethodKind => this._builderData.MethodKind;

    public OperatorKind OperatorKind => this._builderData.OperatorKind;

    [Memo]
    public IMethod Definition => this.Compilation.Factory.GetAccessor( this._builderData );

    [Memo]
    private IFullRef<IMethod> Ref => this.RefFactory.FromIntroducedDeclaration<IMethod>( this );

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    IRef<IMember> IMember.ToRef() => this.Ref;

    IMethod IMethod.MakeGenericInstance( IReadOnlyList<IType> typeArguments ) => throw new NotSupportedException();

    IRef<IMemberOrNamedType> IMemberOrNamedType.ToRef() => this.Ref;

    public IRef<IMethod> ToRef() => this.Ref;

    IRef<IMethodBase> IMethodBase.ToRef() => this.Ref;

    IMemberOrNamedType IMemberOrNamedType.Definition => this.Definition;

    IMember IMember.Definition => this.Definition;

    bool IMethod.IsExtern => false;

    public IMethodInvoker With( InvokerOptions options ) => new MethodInvoker( this, options );

    public IMethodInvoker With( object? target, InvokerOptions options ) => new MethodInvoker( this, options, target );

    public IMethodInvoker With( IExpression target, InvokerOptions options = default ) => new MethodInvoker( this, options, target );

    public IExpression CreateInvokeExpression( IEnumerable<IExpression> args ) => new MethodInvoker( this ).CreateInvokeExpression( args );

    public object? Invoke( params object?[] args ) => new MethodInvoker( this ).Invoke( args );

    public object? Invoke( IEnumerable<IExpression> args ) => new MethodInvoker( this ).Invoke( args );

    [Memo]
    public IParameter ReturnParameter => new IntroducedParameter( this._builderData.ReturnParameter, this.Compilation, this.GenericContext, this );

    [Memo]
    public IType ReturnType => this.MapType( this._builderData.ReturnParameter.Type );

    public ITypeParameterList TypeParameters => TypeParameterList.Empty;

    IReadOnlyList<IType> IGeneric.TypeArguments => [];

    public bool IsGeneric => false;

    public bool IsCanonicalGenericInstance => this.DeclaringType.IsCanonicalGenericInstance;

    [Memo]
    public IMethod? OverriddenMethod => this.MapDeclaration( this._builderData.OverriddenMethod );

    public INamedType DeclaringType => this._introducedMember.DeclaringType;

    [Memo]
    public IReadOnlyList<IMethod> ExplicitInterfaceImplementations => this.MapDeclarationList( this._builderData.ExplicitInterfaceImplementations );

    public MethodInfo ToMethodInfo() => throw new NotImplementedException();

    IHasAccessors IMethod.DeclaringMember => (IHasAccessors) this._introducedMember;

    public override IDeclaration ContainingDeclaration => this._introducedMember;

    public MethodBase ToMethodBase() => CompileTimeMethodInfo.Create( this );

    public MemberInfo ToMemberInfo() => throw new NotImplementedException();

    ExecutionScope IMemberOrNamedType.ExecutionScope => ExecutionScope.RunTime;

    [Memo]
    public IMember? OverriddenMember => this.MapDeclaration( this._builderData.OverriddenMember );

    public bool? IsIteratorMethod => this._builderData.IsIteratorMethod;

    public override bool CanBeInherited => this._introducedMember.CanBeInherited;

    public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default )
    {
        if ( !this.CanBeInherited )
        {
            return [];
        }
        else
        {
            return SourceMember.GetDerivedDeclarationsCore( this, options );
        }
    }
}