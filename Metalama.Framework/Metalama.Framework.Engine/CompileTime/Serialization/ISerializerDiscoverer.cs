// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Engine.CompileTime.Serialization
{
    /// <summary>
    /// Exposes a method <seealso cref="DiscoverSerializers"/> that allows implementations
    /// of the <seealso cref="ISerializerFactoryProvider"/> interface to discover serializer types
    /// for each type being serialized.
    /// </summary>
    /// <remarks>
    /// Only implementations of <seealso cref="ISerializerFactoryProvider"/> may implement this interface.
    /// </remarks>
    /// <seealso cref="ISerializerFactoryProvider"/>
    internal interface ISerializerDiscoverer
    {
        /// <summary>
        /// Invoked by <seealso cref="CompileTimeSerializer"/> once for every type that needs to be serialized.
        /// </summary>
        /// <param name="objectType">Type being serialized.</param>
        void DiscoverSerializers( Type objectType );
    }
}