// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;

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

    public abstract IReadOnlyList<IRef<IMember>> ExplicitInterfaceImplementationMembers { get; }

    public new IFullRef<INamedType> DeclaringType => (IFullRef<INamedType>) this.ContainingDeclaration;
    
    public override string ToString() => this.ContainingDeclaration + "." + this.Name;
}