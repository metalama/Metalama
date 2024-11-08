// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

internal abstract class MemberOrNamedTypeBuilderData( IMemberOrNamedTypeBuilderImpl builder, IFullRef<IDeclaration> containingDeclaration )
    : NamedDeclarationBuilderData( builder, containingDeclaration )
{
    public Accessibility Accessibility { get; } = builder.Accessibility;

    public bool IsSealed { get; } = builder.IsSealed;

    public bool IsNew { get; } = builder.IsNew;

    public bool HasNewKeyword { get; } = builder.HasNewKeyword.AssertNotNull();

    public bool IsAbstract { get; } = builder.IsAbstract;

    public bool IsStatic { get; } = builder.IsStatic;

    public bool IsPartial { get; } = builder.IsPartial;

    public override IFullRef<INamedType>? DeclaringType => this.ContainingDeclaration as IFullRef<INamedType>;
}