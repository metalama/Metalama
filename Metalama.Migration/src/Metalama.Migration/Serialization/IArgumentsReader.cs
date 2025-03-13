// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using PostSharp.Constraints;
using PostSharp.Reflection;

namespace PostSharp.Serialization
{
    /// <summary>
    /// In Metalama, use <see cref="Metalama.Framework.Serialization.IArgumentsReader"/>.
    /// </summary>
    [InternalImplement]
    public interface IArgumentsReader
    {
        bool TryGetValue<T>( string name, out T value, string scope = null );

        T GetValue<T>( string name, string scope = null );

        IMetadataDispenser MetadataDispenser { get; }
    }
}