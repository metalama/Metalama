// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.Utilities;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

internal abstract class IntroducedMember : IntroducedMemberOrNamedType, IMemberImpl
{
    protected IntroducedMember( CompilationModel compilation, IGenericContext genericContext ) : base( compilation, genericContext ) { }

    protected abstract MemberBuilderData MemberBuilderData { get; }

    public abstract bool IsExplicitInterfaceImplementation { get; }

    public new INamedType DeclaringType => base.DeclaringType.AssertNotNull();

    public bool IsVirtual => this.MemberBuilderData.IsVirtual;

    public bool IsAsync => this.MemberBuilderData.IsAsync;

    public bool IsOverride => this.MemberBuilderData.IsOverride;

    public bool HasImplementation => !this.IsAbstract && !this.IsExtern && !this.IsPartial;

    public bool IsExtern => this.MemberBuilderData.IsExtern;

    public sealed override bool CanBeInherited => (this.IsAbstract || this.IsVirtual || this.IsOverride) && !this.IsSealed;

    [Memo]
    public IMember? OverriddenMember => this.MapDeclaration( this.MemberBuilderData.OverriddenMember );

    /// <inheritdoc/>
    /// <remarks>
    /// The enumeration reads <see cref="DeclaringType"/>, which resolves a reference and therefore throws when the
    /// declaring type is absent from the compilation this member is read in. That absence is an error situation, and
    /// <see cref="IntroducedMemberOrNamedType.TryGetDeclaringType"/> logs it, but it does not have to terminate the
    /// pipeline here: a member whose declaring type is absent from a compilation has no derived declaration in that
    /// compilation, so the empty result is correct. See issue #2048.
    /// </remarks>
    public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default )
    {
        if ( !this.CanBeInherited || !this.TryGetDeclaringType( out _ ) )
        {
            return [];
        }
        else
        {
            return SourceMember.GetDerivedDeclarationsCore( this, options );
        }
    }

    IMember IMember.Definition => this;

    IRef<IMember> IMember.ToRef() => this.ToMemberFullRef();

    protected abstract IFullRef<IMember> ToMemberFullRef();
}