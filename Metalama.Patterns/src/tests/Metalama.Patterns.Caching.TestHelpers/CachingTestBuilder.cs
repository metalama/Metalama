// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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