// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

internal abstract class MemberOrNamedTypeBuilderData : NamedDeclarationBuilderData
{
    protected MemberOrNamedTypeBuilderData( IMemberOrNamedTypeBuilderImpl builder, IFullRef<IDeclaration> containingDeclaration ) : base(
        builder,
        containingDeclaration )
    {
        this.Accessibility = builder.Accessibility;
        this.IsSealed = builder.IsSealed;
        this.IsNew = builder.IsNew;
        this.HasNewKeyword = builder.HasNewKeyword.AssertNotNull();
        this.IsAbstract = builder.IsAbstract;
        this.IsStatic = builder.IsStatic;
        this.IsPartial = builder.IsPartial;
    }

    public Accessibility Accessibility { get; }

    public bool IsSealed { get; }

    public bool IsNew { get; }

    public bool HasNewKeyword { get; }

    public bool IsAbstract { get; }

    public bool IsStatic { get; }

    public bool IsPartial { get; }

    public override IFullRef<INamedType>? DeclaringType => this.ContainingDeclaration as IFullRef<INamedType>;
}