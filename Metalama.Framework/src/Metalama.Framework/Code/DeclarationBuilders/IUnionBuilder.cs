// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Utilities;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Allows to complete the construction of a union that has been created by an advice.
/// </summary>
/// <remarks>
/// <para>
/// The union is written with the <c>union</c> keyword. The language applies the restrictions on the members of a
/// union to that form, so an instance field, an automatic property and a field-like event may not be introduced
/// into it.
/// </para>
/// <para>
/// A union requires at least one case, so an advice whose callback adds none reports an error.
/// </para>
/// <para>
/// The members that the compiler synthesizes for a union, which are one constructor per case and the <c>Value</c>
/// property, are not set through this interface. They are created by the advice and are read through
/// <see cref="INamedType.Facets"/> on the introduced type.
/// </para>
/// </remarks>
/// <seealso cref="Metalama.Framework.Code.Types.IUnionFacet"/>
/// <seealso href="@introducing-types"/>
[InternalImplement]
public interface IUnionBuilder : INamedTypeBuilder
{
    /// <summary>
    /// Gets the case types that have been added so far, in the order in which they were added.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The list holds types and not cases. A case of a union is a type and nothing else, so there is nothing else
    /// for an element of this list to carry. <see cref="Metalama.Framework.Code.Types.IUnionCase"/>, which the
    /// introduced type reports, carries the index and the creation member in addition, and neither exists while the
    /// union is being built.
    /// </para>
    /// </remarks>
    /// <seealso cref="Metalama.Framework.Code.Types.IUnionFacet.Cases"/>
    IReadOnlyList<IType> Cases { get; }

    /// <summary>
    /// Adds a case to the union.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The compiler reports the case types of a union as a set, so adding a case whose type is already a case of
    /// this union throws an <see cref="ArgumentException"/> rather than declaring a second case.
    /// </para>
    /// <para>
    /// The type of a case is an ordinary type declared elsewhere, and the union declares no type itself.
    /// </para>
    /// </remarks>
    /// <param name="caseType">The type of the case.</param>
    void AddCase( IType caseType );

    /// <inheritdoc cref="AddCase(IType)"/>
    void AddCase( Type caseType );
}
