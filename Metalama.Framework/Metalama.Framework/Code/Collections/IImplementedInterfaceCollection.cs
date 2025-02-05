// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Collections
{
    /// <summary>
    /// List of interfaces implemented by a named type.
    /// </summary>
    /// <remarks>
    ///  <para>The order of items in this list is undetermined and may change between versions.</para>
    /// </remarks>
    public interface IImplementedInterfaceCollection : IReadOnlyCollection<INamedType>
    {
        bool Contains( INamedType namedType );

        /// <summary>
        /// Determines whether the current collection contains a given <see cref="Type"/>.
        /// </summary>
        bool Contains( Type type );
    }
}