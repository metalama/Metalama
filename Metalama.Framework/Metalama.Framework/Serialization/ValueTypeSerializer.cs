// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Serialization
{
    public abstract class ValueTypeSerializer<T> : ISerializer
        where T : struct
    {
        bool ISerializer.IsTwoPhase => false;

        /// <summary>
        /// Serializes an object.
        /// </summary>
        /// <param name="obj"></param>
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

        /// <inheritdoc />
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