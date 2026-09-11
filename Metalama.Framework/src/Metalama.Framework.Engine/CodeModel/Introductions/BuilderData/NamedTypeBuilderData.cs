// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
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

    /// <summary>
    /// Gets the underlying integral type of an enum, or <c>null</c> for every other kind, which is its own underlying
    /// type.
    /// </summary>
    public IFullRef<INamedType>? UnderlyingType { get; }

    public bool IsReadOnly { get; }

    public bool IsRef { get; }

    /// <summary>
    /// Gets the names of the members of an enum, in the order in which the aspect added them, or an empty array for
    /// every other kind.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order is stored here because it cannot be recovered afterwards. The members reach the compilation model as
    /// separate transformations, and the order of <see cref="INamedType.Fields"/> depends on which fields a previous
    /// consumer resolved by name, which is why <c>EnumFacet</c> takes the order of a type read from source from its
    /// symbol rather than from that collection.
    /// </para>
    /// </remarks>
    public ImmutableArray<string> EnumMemberNames { get; }

    public NamedTypeBuilderData( NamedTypeBuilder builder, IFullRef<IDeclaration> containingDeclaration ) : base( builder, containingDeclaration )
    {
        this._ref = builder.Ref;
        this.BaseType = builder.BaseType?.ToFullRef();
        this.TypeParameters = builder.TypeParameters.ToImmutable( this._ref );
        this.ImplementedInterfaces = builder.ImplementedInterfaces.SelectAsImmutableArray( i => i.ToFullRef() );
        this.Attributes = builder.Attributes.ToImmutable( this._ref );
        this.TypeKind = builder.TypeKind;
        this.IsRecord = builder.IsRecord;
        this.IsClosed = builder.IsClosed;
        this.IsUnion = builder.IsUnion;
        this.IsReadOnly = builder.IsReadOnly;
        this.IsRef = builder.IsRef;

        // An enum reports its underlying integral type here; every other kind is its own underlying type and stores
        // nothing, so that the common case carries no reference.
        this.UnderlyingType = builder.TypeKind == TypeKind.Enum ? builder.UnderlyingType.ToFullRef() : null;

        this.EnumMemberNames = builder is EnumBuilder enumBuilder
            ? enumBuilder.MemberBuilders.SelectAsImmutableArray( m => m.Name )
            : ImmutableArray<string>.Empty;
    }

    protected override IFullRef<IDeclaration> ToDeclarationFullRef() => this._ref;

    public new IFullRef<INamedType> ToRef() => this._ref;

    public override DeclarationKind DeclarationKind => DeclarationKind.NamedType;

    public bool IsRecord { get; }

    public bool IsClosed { get; }

    public bool IsUnion { get; }

    public override IEnumerable<DeclarationBuilderData> GetOwnedDeclarations() => base.GetOwnedDeclarations().Concat( this.TypeParameters );
}