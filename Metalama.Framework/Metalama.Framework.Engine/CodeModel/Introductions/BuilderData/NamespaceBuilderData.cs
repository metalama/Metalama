// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

internal sealed class NamespaceBuilderData : NamedDeclarationBuilderData
{
    private readonly IntroducedRef<INamespace> _ref;

    public string FullName { get; }

    public NamespaceBuilderData( NamespaceBuilder builder, IFullRef<IDeclaration> containingDeclaration ) : base( builder, containingDeclaration )
    {
        this._ref = builder.Ref;
        this.FullName = builder.FullName;
        this.Attributes = ImmutableArray<AttributeBuilderData>.Empty;
    }

    protected override IFullRef<IDeclaration> ToDeclarationFullRef() => this._ref;

    public override IFullRef<INamedType>? DeclaringType => null;

    public new IFullRef<INamespace> ToRef() => this._ref;

    public override DeclarationKind DeclarationKind => DeclarationKind.Namespace;

    public override int GetHashCode() => this.FullName.GetHashCodeOrdinal();

    public override bool Equals( DeclarationBuilderData? other )
        => other is NamespaceBuilderData otherNs && this.FullName.Equals( otherNs.FullName, StringComparison.Ordinal );
}