// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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