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
/// The immutable data of an introduced record, in either of its two authoring forms.
/// </summary>
/// <remarks>
/// <para>
/// Every member below is synthesized by the compiler from the record declaration and is emitted by nothing, so the
/// builder data is the only place that records it. Two of them cannot be recovered from the code model at all: the
/// name of the clone method is not a C# identifier, so <see cref="INamedType.Methods"/> never contains it, and a
/// positional property is told from an ordinary one by its declaring syntax, which an introduced property does not
/// have.
/// </para>
/// </remarks>
internal sealed class RecordBuilderData : NamedTypeBuilderData
{
    /// <summary>
    /// Gets the primary constructor of the record, whose parameters are its positional parameters.
    /// </summary>
    public IFullRef<IConstructor> PrimaryConstructor { get; }

    /// <summary>
    /// Gets the property carrying the equality contract, or <c>null</c> for a record struct, which has none.
    /// </summary>
    public IFullRef<IProperty>? EqualityContractProperty { get; }

    /// <summary>
    /// Gets the method printing the members of the record.
    /// </summary>
    public IFullRef<IMethod> PrintMembersMethod { get; }

    /// <summary>
    /// Gets the clone method, or <c>null</c> for a record struct, which has none.
    /// </summary>
    public IFullRef<IMethod>? CloneMethod { get; }

    /// <summary>
    /// Gets the copy constructor, or <c>null</c> for a record struct, which has none.
    /// </summary>
    public IFullRef<IConstructor>? CopyConstructor { get; }

    /// <summary>
    /// Gets the deconstructing method, or <c>null</c> when the record declares no positional parameter.
    /// </summary>
    public IFullRef<IMethod>? DeconstructMethod { get; }

    /// <summary>
    /// Gets the properties that the positional parameters declare, in order.
    /// </summary>
    public ImmutableArray<IFullRef<IProperty>> PositionalProperties { get; }

    public RecordBuilderData( RecordBuilder builder, IFullRef<IDeclaration> containingDeclaration ) : base( builder, containingDeclaration )
    {
        this.PrimaryConstructor = builder.PrimaryConstructorBuilder.BuilderData.ToRef();
        this.EqualityContractProperty = builder.EqualityContractProperty?.BuilderData.ToRef();
        this.PrintMembersMethod = builder.PrintMembersMethod.AssertNotNull().BuilderData.ToRef();
        this.CloneMethod = builder.CloneMethod?.BuilderData.ToRef();
        this.CopyConstructor = builder.CopyConstructor?.BuilderData.ToRef();
        this.DeconstructMethod = builder.DeconstructMethod?.BuilderData.ToRef();
        this.PositionalProperties = builder.PositionalPropertyBuilders.SelectAsImmutableArray( p => (IFullRef<IProperty>) p.BuilderData.ToRef() );
    }
}
