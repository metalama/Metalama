// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Diagnostics.CodeAnalysis;
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

    /// <summary>
    /// Sets <paramref name="declaringType"/> to the declaring type of this declaration and returns <c>true</c>, or
    /// returns <c>false</c> when the reference to the declaring type does not resolve in the compilation this
    /// declaration is read in.
    /// </summary>
    /// <remarks>
    /// A builder is consistent with the compilation model that produced it, while a facade such as this one is read
    /// in a consuming compilation model, and the two are not necessarily the same compilation.
    /// <see cref="DeclaringType"/> throws <see cref="SymbolNotFoundException"/> when they differ and the consuming
    /// compilation does not contain the declaring type. This method is the non-throwing form, for the callers that
    /// treat an absent declaring type as a normal outcome. See issue #2048.
    /// </remarks>
    public bool TryGetDeclaringType( [NotNullWhen( true )] out INamedType? declaringType )
    {
        declaringType = this.MemberOrNamedTypeBuilderData.DeclaringType?.GetTargetOrNull( this.Compilation, this.GenericContext );

        return declaringType != null;
    }

    public MemberInfo ToMemberInfo() => throw new NotImplementedException();

    ExecutionScope IMemberOrNamedType.ExecutionScope => ExecutionScope.RunTime;

    IMemberOrNamedType IMemberOrNamedType.Definition => this.GetDefinition();

    protected abstract IMemberOrNamedType GetDefinition();

    IRef<IMemberOrNamedType> IMemberOrNamedType.ToRef() => this.ToFullDeclarationRef().As<IMemberOrNamedType>();
}