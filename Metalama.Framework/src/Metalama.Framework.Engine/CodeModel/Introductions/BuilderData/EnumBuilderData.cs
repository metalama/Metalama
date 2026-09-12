// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

/// <summary>
/// The immutable data of an introduced enum.
/// </summary>
internal sealed class EnumBuilderData : NamedTypeBuilderData
{
    /// <summary>
    /// Gets the underlying integral type of the enum.
    /// </summary>
    public IFullRef<INamedType> UnderlyingType { get; }

    /// <summary>
    /// Gets the members of the enum, in the order in which the aspect added them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order is stored here because it cannot be recovered afterwards. The members reach the compilation model
    /// as separate transformations, and the order of <see cref="INamedType.Fields"/> depends on which fields a
    /// previous consumer resolved by name, which is why <c>SourceEnumFacet</c> takes the order of a type read from
    /// source from its symbol rather than from that collection.
    /// </para>
    /// <para>
    /// They are stored as references rather than as names so that the facet can resolve them in any compilation
    /// that knows the enum, including one in which the transformations that register them have not been applied. An
    /// aspect reads the facet of a type it has just introduced through a builder whose compilation is the one the
    /// aspect sees, which is not the compilation the advice writes to.
    /// </para>
    /// </remarks>
    public ImmutableArray<IFullRef<IField>> Members { get; }

    public EnumBuilderData( EnumBuilder builder, IFullRef<IDeclaration> containingDeclaration ) : base( builder, containingDeclaration )
    {
        this.UnderlyingType = builder.UnderlyingType.ToFullRef();
        this.Members = builder.MemberBuilders.SelectAsImmutableArray( m => (IFullRef<IField>) m.BuilderData.ToRef() );
    }
}
