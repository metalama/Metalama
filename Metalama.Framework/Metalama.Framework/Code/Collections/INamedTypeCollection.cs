// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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