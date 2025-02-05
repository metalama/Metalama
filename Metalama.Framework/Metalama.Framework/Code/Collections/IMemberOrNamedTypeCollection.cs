// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Collections;

/// <summary>
/// Read-only list of <see cref="IMemberOrNamedType"/>.
/// </summary>
/// <remarks>
///  <para>The order of items in this list is undetermined and may change between versions.</para>
/// </remarks>
public interface IMemberOrNamedTypeCollection<out T> : INamedDeclarationCollection<T>
    where T : IMemberOrNamedType { }

/// <summary>
/// A collection of named elements.
/// </summary>
[InternalImplement]
[CompileTime]
public interface INamedDeclarationCollection<out T> : IReadOnlyCollection<T>
    where T : INamedDeclaration
{
    /// <summary>
    /// Gets the set of members of a given name. Note that for named types, the short name will be matched,
    /// as opposed to the full, namespace-prefixed name.
    /// </summary>
    /// <param name="name">The member name (not including the namespace, for types).</param>
    IEnumerable<T> OfName( string name );
}