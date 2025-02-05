// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Serialization
{
    /// <summary>
    /// Defines the semantics of an object serializer.
    /// </summary>
    [CompileTime]
    public interface ISerializer
    {
        // ReSharper disable once UnusedParameter.Global

        /// <summary>
        /// Converts a value into a given target type.
        /// </summary>
        /// <param name="value">Value to be converted.</param>
        /// <param name="targetType">target type.</param>
        /// <returns>The <paramref name="value"/> converted to <paramref name="targetType"/>.</returns>
        /// <remarks>
        ///     <para>This method allows for additional flexibility if the serialization-time type is
        ///             not equal to the deserialization-time. The current method is invoked
        /// at deserialization time to perform the conversion.</para>
        /// </remarks>
        object Convert( object value, Type targetType );

        // ReSharper disable once UnusedParameter.Global

        /// <summary>
        /// Creates an instance of the given type.
        /// </summary>
        /// <param name="type">Type of the instance to be created.</param>
        /// <param name="constructorArguments">Gives access to arguments required to create the instance.</param>
        /// <returns>An instance of type <paramref name="type"/> initialized using <paramref name="constructorArguments"/>.</returns>
        /// <remarks>
        ///     <para>This method is invoked during deserialization. If <see cref="IsTwoPhase"/> is <c>true</c>, the <see cref="DeserializeFields"/>
        ///     method is called later to complete deserialization.</para>
        /// </remarks>
        object CreateInstance( Type type, IArgumentsReader constructorArguments );

        /// <summary>
        /// Completes the second phase of deserialization by setting fields and other properties.
        /// </summary>
        /// <param name="obj">The object being deserialized.</param>
        /// <param name="initializationArguments">Gives access to field values.</param>
        /// <remarks>
        ///     <para>This method is only invoked if <see cref="IsTwoPhase"/> is <c>true</c>.</para>
        /// </remarks>
        void DeserializeFields( ref object obj, IArgumentsReader initializationArguments );

        /// <summary>
        /// Serializes an object.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="constructorArguments">Gives access to arguments that will be passed to the <see cref="CreateInstance"/> method during deserialization.</param>
        /// <param name="initializationArguments">Gives access to arguments that will be passed to the <see cref="DeserializeFields"/> method during deserialization.</param>
        void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments );

        /// <summary>
        /// Gets a value indicating whether <see cref="DeserializeFields"/> should be invoked during deserialization.
        /// The value is <c>false</c> if <see cref="CreateInstance"/> return fully created object, <c>true</c> otherwise.
        /// </summary>
        bool IsTwoPhase { get; }
    }
}