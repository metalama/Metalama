// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;

namespace Metalama.Framework.Code.Collections
{
    /// <summary>
    /// Read-only list of <see cref="INamedType"/>.
    /// </summary>
    /// <remarks>
    ///  <para>The order of items in this list is undetermined and may change between versions.</para>
    /// </remarks>
    public interface INamedTypeCollection : IMemberOrNamedTypeCollection<INamedType>
    {
        /// <summary>
        /// Gets the types in the collection that are derived from a given generic type,
        /// taking any type instance into account.
        /// </summary>
        /// <param name="typeDefinition"></param>
        /// <returns></returns>
        IEnumerable<INamedType> OfTypeDefinition( INamedType typeDefinition );
    }
}