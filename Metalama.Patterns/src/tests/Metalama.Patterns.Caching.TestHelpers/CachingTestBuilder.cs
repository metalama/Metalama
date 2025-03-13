// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Formatters;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Formatters;

namespace Metalama.Patterns.Caching.TestHelpers;

public sealed class CachingTestBuilder
{
    private readonly ICachingServiceBuilder _serviceBuilder;

    internal CachingTestBuilder( ICachingServiceBuilder serviceBuilder )
    {
        this._serviceBuilder = serviceBuilder;
    }

    public CachingTestBuilder WithProfile( string name ) => this.WithProfile( new CachingProfile( name ) );

    public CachingTestBuilder WithProfile( CachingProfile profile )
    {
        this._serviceBuilder.AddProfile( profile );

        return this;
    }

    public CachingTestBuilder WithKeyBuilder( Func<IFormatterRepository, CacheKeyBuilderOptions, CacheKeyBuilder> factory )
    {
        this._serviceBuilder.WithKeyBuilder( factory );

        return this;
    }
}