// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Serialization
{
    /// <summary>
    /// Provides read access to the collection of deserialized arguments.
    /// </summary>
    [CompileTime]
    public interface IArgumentsReader
    {
        /// <summary>
        /// Attempts to read a value from the collection, and does not throw an exception if the value does not exist or is <c>null</c>.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="name">Argument name.</param>
        /// <param name="value">At output, set to the value of the argument named <paramref name="name"/> in the given optional <paramref name="scope"/>.</param>
        /// <param name="scope">An optional prefix of <paramref name="name"/>, similar to a namespace.</param>
        /// <returns><c>true</c> if the value is defined, otherwise <c>false</c>.</returns>
        bool TryGetValue<T>( string name, [MaybeNullWhen( false )] out T value, string? scope = null );

        /// <summary>
        /// Reads a value from the collection, and returns <c>null</c> if the value does not exist.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="name">Argument name.</param>
        /// <param name="scope">An optional prefix of <paramref name="name"/>, similar to a namespace.</param>
        /// <returns>The value of the argument named <paramref name="name"/> in the given optional <paramref name="scope"/>.</returns>
        T? GetValue<T>( string name, string? scope = null );
    }
}