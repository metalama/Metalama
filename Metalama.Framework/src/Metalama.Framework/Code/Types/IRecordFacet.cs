// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Utilities;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Types;

/// <summary>
/// Represents the structure of a record: the members that the compiler synthesizes for it.
/// </summary>
/// <remarks>
/// <para>
/// The facet is reached through <see cref="ITypeFacetCollection.Record"/>, as in
/// <c>type.Facets.Record?.PrintMembersMethod</c>. It is <c>null</c> for a type that
/// <see cref="INamedType.IsRecord"/> reports as not being a record.
/// </para>
/// <para>
/// One interface describes a record class and a record struct. A record struct has no equality contract, no clone
/// method and no copy constructor, so these three properties are <c>null</c> for it.
/// </para>
/// <para>
/// The members that the compiler adds to a record and that are not specific to a record are not part of the facet:
/// <c>Equals</c>, <c>GetHashCode</c>, <c>ToString</c>, and the <c>==</c> and <c>!=</c> operators are reached through
/// the member collections of <see cref="INamedType"/> like any other member. The primary constructor is reached
/// through <see cref="INamedType.PrimaryConstructor"/>, because a primary constructor is not specific to a record
/// since C# 12.
/// </para>
/// </remarks>
/// <seealso cref="INamedType.IsRecord"/>
/// <seealso cref="INamedType.Facets"/>
[CompileTime]
[InternalImplement]
public interface IRecordFacet : ITypeFacet
{
    /// <summary>
    /// Gets the <c>EqualityContract</c> property, or <c>null</c> when the type is a record struct, which has none.
    /// </summary>
    IProperty? EqualityContractProperty { get; }

    /// <summary>
    /// Gets the <c>PrintMembers</c> method, which appends the members of the record to the string that
    /// <c>ToString</c> returns.
    /// </summary>
    IMethod PrintMembersMethod { get; }

    /// <summary>
    /// Gets the clone method, whose metadata name is <c>&lt;Clone&gt;$</c>, or <c>null</c> when the type is a record
    /// struct, which has none. The <c>with</c> expression of a record class calls this method.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The name of this method is not a C# identifier, so the method is absent from
    /// <see cref="INamedType.Methods"/>, which represents what can be written in C#. This property is the only way to
    /// reach it.
    /// </para>
    /// </remarks>
    IMethod? CloneMethod { get; }

    /// <summary>
    /// Gets the copy constructor, which the clone method calls, or <c>null</c> when the type is a record struct,
    /// which has none.
    /// </summary>
    IConstructor? CopyConstructor { get; }

    /// <summary>
    /// Gets the <c>Deconstruct</c> method, or <c>null</c> when the record is not positional, that is, when it
    /// declares no parameter list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A record read from a compiled assembly is reported as not positional, because a compiled assembly does not
    /// record the parameter list of a record. The <c>Deconstruct</c> method of such a record is a member of the type
    /// and is reached through <see cref="INamedType.Methods"/>.
    /// </para>
    /// </remarks>
    IMethod? DeconstructMethod { get; }

    /// <summary>
    /// Gets the properties that the positional parameters of the record declare, in the order of the parameters, or
    /// an empty list when the record is not positional. A positional parameter that declares no property, because
    /// the record or one of its base records declares a member of that name itself, contributes no element.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A record read from a compiled assembly has an empty list, for the reason that
    /// <see cref="DeconstructMethod"/> gives.
    /// </para>
    /// </remarks>
    IReadOnlyList<IProperty> PositionalProperties { get; }
}
