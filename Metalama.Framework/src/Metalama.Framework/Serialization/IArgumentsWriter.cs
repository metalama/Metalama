// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Serialization
{
    /// <summary>
    /// Provides write access to a collection of arguments that need to be serialized.
    /// </summary>
    [CompileTime]
    public interface IArgumentsWriter
    {
        /// <summary>
        /// Sets the value of an argument.
        /// </summary>
        /// <param name="name">Argument name.</param>
        /// <param name="value">Argument value. The value can be <c>null</c> or must be serializable.</param>
        /// <param name="scope">An optional prefix of <paramref name="name"/>, similar to a namespace.</param>
        void SetValue( string name, object? value, string? scope = null );
    }
}