// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Tests.Backends;

public sealed partial class LayeredCachingBackendEnhancerTests
{
    /// <summary>
    /// A wrapper backend that allows configuring feature flags (blocking, dependencies support)
    /// for testing different code paths in <see cref="LayeredCachingBackendEnhancer"/>.
    /// </summary>
    private sealed class ConfigurableFeaturesBackend : CachingBackendEnhancer
    {
        private readonly bool _blocking;
        private readonly bool _supportsDependencies;

        public ConfigurableFeaturesBackend(
            CachingBackend underlyingBackend,
            bool blocking = true,
            bool supportsDependencies = true )
            : base( underlyingBackend )
        {
            this._blocking = blocking;
            this._supportsDependencies = supportsDependencies;
        }

        protected override CachingBackendFeatures CreateFeatures()
        {
            return new ConfigurableFeatures( this._blocking, this._supportsDependencies );
        }

        private sealed class ConfigurableFeatures : CachingBackendFeatures
        {
            public ConfigurableFeatures( bool blocking, bool supportsDependencies )
            {
                this.Blocking = blocking;
                this.Dependencies = supportsDependencies;
                this.ContainsDependency = supportsDependencies;
            }

            public override bool Blocking { get; }

            public override bool Dependencies { get; }

            public override bool ContainsDependency { get; }
        }
    }
}