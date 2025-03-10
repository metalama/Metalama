// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Serialization
{
    /// <summary>
    /// Defines a method <see cref="CreateSerializer"/>, which creates instances of the <see cref="ISerializer"/> interface for
    /// given object types.
    /// </summary>
    [CompileTime]
    public interface ISerializerFactory
    {
        /// <summary>
        /// Creates an instance of the <see cref="ISerializer"/> interface for a given object type.
        /// </summary>
        /// <param name="objectType">Type of object being serialized or deserialized.</param>
        /// <returns>A new instance implementing the <see cref="ISerializer"/> interface.</returns>
        ISerializer CreateSerializer( Type objectType );
    }
}