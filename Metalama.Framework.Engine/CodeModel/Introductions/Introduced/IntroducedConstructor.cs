// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Invokers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

internal sealed class IntroducedConstructor : IntroducedMember, IConstructorImpl
{
    private readonly ConstructorBuilderData _builderData;

    public IntroducedConstructor( ConstructorBuilderData builderData, CompilationModel compilation, IGenericContext genericContext ) : base(
        compilation,
        genericContext )
    {
        this._builderData = builderData;
    }

    public override DeclarationBuilderData BuilderData => this._builderData;

    protected override NamedDeclarationBuilderData NamedDeclarationBuilderData => this._builderData;

    protected override MemberOrNamedTypeBuilderData MemberOrNamedTypeBuilderData => this._builderData;

    protected override MemberBuilderData MemberBuilderData => this._builderData;

    public override bool IsExplicitInterfaceImplementation => false;

    public override bool IsImplicitlyDeclared => this._builderData.IsImplicitlyDeclared;

    [Memo]
    public IParameterList Parameters
        => new ParameterList(
            this,
            this.Compilation.GetParameterCollection( this._builderData.ToRef() ) );

    public MethodBase ToMethodBase() => this.ToConstructorInfo();

    IRef<IMethodBase> IMethodBase.ToRef() => this.ToRef();

    protected override IFullRef<IMember> ToMemberFullRef() => this.Ref;

    [Memo]
    private IFullRef<IConstructor> Ref
        => this._builderData.ReplacedImplicitConstructor?.WithGenericContext( this.GenericContext )
           ?? this.RefFactory.FromIntroducedDeclaration<IConstructor>( this );

    public IRef<IConstructor> ToRef() => this.Ref;

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    public ConstructorInitializerKind InitializerKind => this._builderData.InitializerKind;

    bool IConstructor.IsPrimary => false;

    public ConstructorInfo ToConstructorInfo() => CompileTimeConstructorInfo.Create( this );

    [Memo]
    public IConstructor Definition => this.Compilation.Factory.GetConstructor( this._builderData ).AssertNotNull();

    protected override IMemberOrNamedType GetDefinition() => this.Definition;

    public IConstructor? GetBaseConstructor()
        =>

            // Currently ConstructorBuilder is used to represent a default constructor, the base constructor is always
            // the default constructor of the base class.
            this.DeclaringType.BaseType?.Constructors.SingleOrDefault( c => c.Parameters.Count == 0 );

    public object Invoke( params object?[] args ) => new ConstructorInvoker( this ).Invoke( args );

    public object Invoke( IEnumerable<IExpression> args ) => new ConstructorInvoker( this ).Invoke( args );

    public IObjectCreationExpression CreateInvokeExpression() => new ConstructorInvoker( this ).CreateInvokeExpression();

    public IObjectCreationExpression CreateInvokeExpression( params object?[] args ) => new ConstructorInvoker( this ).CreateInvokeExpression( args );

    public IObjectCreationExpression CreateInvokeExpression( params IExpression[] args ) => new ConstructorInvoker( this ).CreateInvokeExpression( args );

    public IObjectCreationExpression CreateInvokeExpression( IEnumerable<IExpression> args ) => new ConstructorInvoker( this ).CreateInvokeExpression( args );
}