// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Serialization;
using Metalama.Framework.Services;
using System;

namespace Metalama.Framework.Engine.CompileTime.Serialization
{
    /// <summary>
    /// Provides instances of the <see cref="ISerializerFactory"/> interface given the object type.
    /// </summary>
    /// <seealso cref="ISerializerDiscoverer"/>
    internal interface ISerializerFactoryProvider : IProjectService
    {
        /// <summary>
        /// Gets the instance of <see cref="ISerializerFactory"/>.
        /// </summary>
        /// <param name="objectType">Type of object being serialized or deserialized.</param>
        /// <returns>An instance of <see cref="ISerializerFactory"/> able to serialize or deserialize <paramref name="objectType"/>, or <c>null</c>
        /// if there is no known serializer factory for this object. </returns>
        /// <remarks>
        /// <para>It is <i>not</i> the responsibility of this class to call the next provider (<see cref="NextProvider"/>).</para>
        /// </remarks>
        ISerializerFactory? GetSerializerFactory( Type objectType );

        /// <summary>
        /// Gets the next provider in the chain.
        /// </summary>
        ISerializerFactoryProvider? NextProvider { get; }
    }
}