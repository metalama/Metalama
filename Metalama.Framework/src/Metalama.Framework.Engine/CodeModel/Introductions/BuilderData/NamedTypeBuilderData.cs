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

/// <summary>
/// The immutable data of an introduced type, which carries what every kind has.
/// </summary>
/// <remarks>
/// <para>
/// A kind that carries more than this has a derived class of its own, which the builder of that kind creates in
/// <see cref="NamedTypeBuilder.CreateBuilderData"/>. The facet of that kind reads the derived class, which is where
/// the members that the compiler synthesizes are recorded.
/// </para>
/// </remarks>
internal class NamedTypeBuilderData : MemberOrNamedTypeBuilderData
{
    private readonly IntroducedRef<INamedType> _ref;

    public IFullRef<INamedType>? BaseType { get; }

    public ImmutableArray<TypeParameterBuilderData> TypeParameters { get; }

    public ImmutableArray<IFullRef<INamedType>> ImplementedInterfaces { get; }

    public TypeKind TypeKind { get; }

    public bool IsReadOnly { get; }

    public bool IsRef { get; }

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
    }

    protected override IFullRef<IDeclaration> ToDeclarationFullRef() => this._ref;

    public new IFullRef<INamedType> ToRef() => this._ref;

    public override DeclarationKind DeclarationKind => DeclarationKind.NamedType;

    public bool IsRecord { get; }

    public bool IsClosed { get; }

    public bool IsUnion { get; }

    public override IEnumerable<DeclarationBuilderData> GetOwnedDeclarations() => base.GetOwnedDeclarations().Concat( this.TypeParameters );
}
