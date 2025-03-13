// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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