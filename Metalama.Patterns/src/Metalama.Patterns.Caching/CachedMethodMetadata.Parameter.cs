// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching;

public partial class CachedMethodMetadata
{
    /// <summary>
    /// Encapsulates information about a parameter of a method
    /// being cached.
    /// </summary>
    private sealed class Parameter
    {
        /// <summary>
        /// Gets a value indicating whether the parameter should be excluded
        /// from the cache key. When the value of this property is <c>false</c>,
        /// the parameter should be included in the cache key.
        /// </summary>
        public bool IsParameterIgnored { get; }

        internal Parameter( bool isIgnored )
        {
            this.IsParameterIgnored = isIgnored;
        }
    }
}