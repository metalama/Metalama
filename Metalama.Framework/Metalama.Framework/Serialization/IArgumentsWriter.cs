// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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