// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

internal abstract class MemberBuilderData( IMemberBuilder builder, IFullRef<IDeclaration> containingDeclaration )
    : MemberOrNamedTypeBuilderData( (IMemberOrNamedTypeBuilderImpl) builder, containingDeclaration )
{
    public bool IsVirtual { get; } = builder.IsVirtual;

    public bool IsAsync { get; } = builder.IsAsync;

    public bool IsOverride { get; } = builder.IsOverride;

    public bool IsExtern { get; } = builder.IsExtern;

    public abstract IRef<IMember>? OverriddenMember { get; }

    public new IFullRef<INamedType> DeclaringType => (IFullRef<INamedType>) this.ContainingDeclaration;
    
    public override string ToString() => this.ContainingDeclaration + "." + this.Name;
}