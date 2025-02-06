// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.Aspects.Configuration;

namespace Metalama.Patterns.Caching.AspectTests.CacheAttributeTests.ParameterFilter_Error;

public class TheCacheParameterClassifier : ICacheParameterClassifier
{
    public CacheParameterClassification GetClassification( IParameter parameter )
        => parameter.Type.IsConvertibleTo( typeof(IDisposable) ) ? CacheParameterClassification.Ineligible() : CacheParameterClassification.Default;
}

public class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.ConfigureCaching( builder => builder.AddParameterClassifier( "IDisposable", new TheCacheParameterClassifier() ) );
    }
}

internal class TheClass
{
    [Cache]

    // y should be marked with [NotCacheKey].
    public int CachedMethod( int x, IDisposable y )
    {
        return x;
    }
}