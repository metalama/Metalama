// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Reflection;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

internal abstract class IntroducedMemberOrNamedType : IntroducedNamedDeclaration, IMemberOrNamedTypeImpl
{
    protected IntroducedMemberOrNamedType( CompilationModel compilation, IGenericContext genericContext ) : base( compilation, genericContext ) { }

    protected abstract MemberOrNamedTypeBuilderData MemberOrNamedTypeBuilderData { get; }

    public Accessibility Accessibility => this.MemberOrNamedTypeBuilderData.Accessibility;

    public bool IsAbstract => this.MemberOrNamedTypeBuilderData.IsAbstract;

    public bool IsStatic => this.MemberOrNamedTypeBuilderData.IsStatic;

    public bool IsSealed => this.MemberOrNamedTypeBuilderData.IsSealed;

    public bool IsNew => this.MemberOrNamedTypeBuilderData.IsNew;

    public bool? HasNewKeyword => this.MemberOrNamedTypeBuilderData.HasNewKeyword;

    public bool IsPartial => this.MemberOrNamedTypeBuilderData.IsPartial;

    [Memo]
    public INamedType? DeclaringType => this.MapDeclaration( this.MemberOrNamedTypeBuilderData.DeclaringType );

    public MemberInfo ToMemberInfo() => throw new NotImplementedException();

    ExecutionScope IMemberOrNamedType.ExecutionScope => ExecutionScope.RunTime;

    IMemberOrNamedType IMemberOrNamedType.Definition => this.GetDefinition();

    protected abstract IMemberOrNamedType GetDefinition();

    IRef<IMemberOrNamedType> IMemberOrNamedType.ToRef() => this.ToFullDeclarationRef().As<IMemberOrNamedType>();
}