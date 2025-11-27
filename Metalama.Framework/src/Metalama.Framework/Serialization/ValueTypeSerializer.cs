// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Serialization
{
    /// <summary>
    /// Base class for serializers of value types that use a single-phase deserialization process.
    /// </summary>
    /// <typeparam name="T">The value type to serialize.</typeparam>
    /// <seealso cref="ReferenceTypeSerializer"/>
    /// <seealso cref="ReferenceTypeSerializer{T}"/>
    /// <seealso cref="ISerializer"/>
    /// <seealso href="@serialization"/>
    public abstract class ValueTypeSerializer<T> : ISerializer
        where T : struct
    {
        bool ISerializer.IsTwoPhase => false;

        /// <summary>
        /// Serializes an object.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="constructorArguments">Gives access to arguments that will be passed to the <see cref="DeserializeObject"/> method during deserialization.</param>
        public abstract void SerializeObject( T obj, IArgumentsWriter constructorArguments );

        /// <summary>
        /// Creates an instance of the given type.
        /// </summary>
        /// <param name="constructorArguments">Gives access to arguments required to create the instance.</param>
        /// <returns>An instance of type <typeparamref name="T"/> initialized using <paramref name="constructorArguments"/>.</returns>
        public abstract T DeserializeObject( IArgumentsReader constructorArguments );

        /// <inheritdoc />
        void ISerializer.SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
        {
            var typedValue = (T) obj;
            this.SerializeObject( typedValue, constructorArguments );
        }

        /// <summary>
        /// Converts a deserialized value to a target type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The target type to convert to.</param>
        /// <returns>The converted value.</returns>
        public virtual object Convert( object value, Type targetType )
        {
            return value;
        }

        /// <inheritdoc />
        object ISerializer.CreateInstance( Type type, IArgumentsReader constructorArguments )
        {
            return this.DeserializeObject( constructorArguments );
        }

        /// <inheritdoc />
        void ISerializer.DeserializeFields( ref object o, IArgumentsReader initializationArguments ) { }
    }
}