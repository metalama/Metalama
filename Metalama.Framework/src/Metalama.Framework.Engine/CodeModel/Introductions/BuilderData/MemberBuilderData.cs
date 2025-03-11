// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

internal abstract class MemberBuilderData : MemberOrNamedTypeBuilderData
{
    protected MemberBuilderData( IMemberBuilder builder, IFullRef<IDeclaration> containingDeclaration ) : base( (IMemberOrNamedTypeBuilderImpl) builder, containingDeclaration )
    {
        this.IsVirtual = builder.IsVirtual;
        this.IsAsync = builder.IsAsync;
        this.IsOverride = builder.IsOverride;
        this.IsExtern = builder.IsExtern;
    }

    public bool IsVirtual { get; }

    public bool IsAsync { get; }

    public bool IsOverride { get; }

    public bool IsExtern { get; }

    public abstract IRef<IMember>? OverriddenMember { get; }

    public new IFullRef<INamedType> DeclaringType => (IFullRef<INamedType>) this.ContainingDeclaration;
    
    public override string ToString() => this.ContainingDeclaration + "." + this.Name;
}