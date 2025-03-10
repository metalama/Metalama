// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

internal sealed class NamedTypeBuilderData : MemberOrNamedTypeBuilderData
{
    private readonly IntroducedRef<INamedType> _ref;

    public IFullRef<INamedType>? BaseType { get; }

    public ImmutableArray<TypeParameterBuilderData> TypeParameters { get; }

    public ImmutableArray<IFullRef<INamedType>> ImplementedInterfaces { get; }

    public TypeKind TypeKind { get; }

    // The following members can return a constant value at the moment.

#pragma warning disable CA1822

    // ReSharper disable once MemberCanBeMadeStatic.Global
    public bool IsReadOnly => false;

    // ReSharper disable once MemberCanBeMadeStatic.Global
    public bool IsRef => false;

#pragma warning restore CA1822

    public NamedTypeBuilderData( NamedTypeBuilder builder, IFullRef<IDeclaration> containingDeclaration ) : base( builder, containingDeclaration )
    {
        this._ref = builder.Ref;
        this.BaseType = builder.BaseType?.ToFullRef();
        this.TypeParameters = builder.TypeParameters.ToImmutable( this._ref );
        this.ImplementedInterfaces = builder.ImplementedInterfaces.SelectAsImmutableArray( i => i.ToFullRef() );
        this.Attributes = builder.Attributes.ToImmutable( this._ref );
        this.TypeKind = builder.TypeKind;
    }

    protected override IFullRef<IDeclaration> ToDeclarationFullRef() => this._ref;

    public new IFullRef<INamedType> ToRef() => this._ref;

    public override DeclarationKind DeclarationKind => DeclarationKind.NamedType;

    public override IEnumerable<DeclarationBuilderData> GetOwnedDeclarations() => base.GetOwnedDeclarations().Concat( this.TypeParameters );
}