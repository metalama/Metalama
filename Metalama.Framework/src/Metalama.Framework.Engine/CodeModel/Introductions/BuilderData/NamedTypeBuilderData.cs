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
    /// Gets the members of an enum, in the order in which the aspect added them, or an empty array for every other
    /// kind.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order is stored here because it cannot be recovered afterwards. The members reach the compilation model as
    /// separate transformations, and the order of <see cref="INamedType.Fields"/> depends on which fields a previous
    /// consumer resolved by name, which is why <c>EnumFacet</c> takes the order of a type read from source from its
    /// symbol rather than from that collection.
    /// </para>
    /// <para>
    /// They are stored as references rather than as names so that the facet can resolve them in any compilation that
    /// knows the enum, including one in which the transformations that register them have not been applied. An
    /// aspect reads the facet of a type it has just introduced through a builder whose compilation is the one the
    /// aspect sees, which is not the compilation the advice writes to.
    /// </para>
    /// </remarks>
    public ImmutableArray<IFullRef<IField>> EnumMembers { get; }

    /// <summary>
    /// Gets the <c>Invoke</c> method of a delegate, or <c>null</c> for every other kind.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is stored for the reason that <see cref="EnumMembers"/> is stored: <c>DelegateFacet</c> resolves the
    /// method through <see cref="INamedType.Methods"/>, which is empty in a compilation to which the transformation
    /// that registers the method has not been applied.
    /// </para>
    /// </remarks>
    public IFullRef<IMethod>? InvokeMethod { get; }

    /// <summary>
    /// Gets the primary constructor of a record, or <c>null</c> for every other kind.
    /// </summary>
    public IFullRef<IConstructor>? PrimaryConstructor { get; }

    /// <summary>
    /// Gets the property carrying the equality contract of a record class, or <c>null</c> for every other kind and
    /// for a record struct.
    /// </summary>
    public IFullRef<IProperty>? EqualityContractProperty { get; }

    /// <summary>
    /// Gets the method printing the members of a record, or <c>null</c> for every other kind.
    /// </summary>
    public IFullRef<IMethod>? PrintMembersMethod { get; }

    /// <summary>
    /// Gets the clone method of a record class, or <c>null</c> for every other kind and for a record struct.
    /// </summary>
    public IFullRef<IMethod>? CloneMethod { get; }

    /// <summary>
    /// Gets the copy constructor of a record class, or <c>null</c> for every other kind and for a record struct.
    /// </summary>
    public IFullRef<IConstructor>? CopyConstructor { get; }

    /// <summary>
    /// Gets the deconstructing method of a positional record, or <c>null</c> for every other kind and for a record
    /// that declares no positional parameter.
    /// </summary>
    public IFullRef<IMethod>? DeconstructMethod { get; }

    /// <summary>
    /// Gets the properties that the positional parameters of a record declare, in order, or an empty array for
    /// every other kind.
    /// </summary>
    public ImmutableArray<IFullRef<IProperty>> PositionalProperties { get; }

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

        this.EnumMembers = builder is EnumBuilder enumBuilder
            ? enumBuilder.MemberBuilders.SelectAsImmutableArray( m => (IFullRef<IField>) m.BuilderData.ToRef() )
            : ImmutableArray<IFullRef<IField>>.Empty;

        this.InvokeMethod = builder is DelegateBuilder delegateBuilder ? delegateBuilder.InvokeMethodBuilder.BuilderData.ToRef() : null;

        if ( builder is RecordBuilder recordBuilder )
        {
            this.PrimaryConstructor = recordBuilder.PrimaryConstructorBuilder.BuilderData.ToRef();
            this.EqualityContractProperty = recordBuilder.EqualityContractProperty?.BuilderData.ToRef();
            this.PrintMembersMethod = recordBuilder.PrintMembersMethod?.BuilderData.ToRef();
            this.CloneMethod = recordBuilder.CloneMethod?.BuilderData.ToRef();
            this.CopyConstructor = recordBuilder.CopyConstructor?.BuilderData.ToRef();
            this.DeconstructMethod = recordBuilder.DeconstructMethod?.BuilderData.ToRef();
            this.PositionalProperties = recordBuilder.PositionalPropertyBuilders.SelectAsImmutableArray( p => (IFullRef<IProperty>) p.BuilderData.ToRef() );
        }
        else
        {
            this.PositionalProperties = ImmutableArray<IFullRef<IProperty>>.Empty;
        }
    }

    protected override IFullRef<IDeclaration> ToDeclarationFullRef() => this._ref;

    public new IFullRef<INamedType> ToRef() => this._ref;

    public override DeclarationKind DeclarationKind => DeclarationKind.NamedType;

    public bool IsRecord { get; }

    public bool IsClosed { get; }

    public bool IsUnion { get; }

    public override IEnumerable<DeclarationBuilderData> GetOwnedDeclarations() => base.GetOwnedDeclarations().Concat( this.TypeParameters );
}