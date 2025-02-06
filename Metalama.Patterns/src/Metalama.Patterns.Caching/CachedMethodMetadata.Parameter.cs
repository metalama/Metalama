// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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