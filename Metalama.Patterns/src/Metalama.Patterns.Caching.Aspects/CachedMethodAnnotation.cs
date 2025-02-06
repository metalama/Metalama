// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Patterns.Caching.Aspects.Configuration;

namespace Metalama.Patterns.Caching.Aspects;

internal sealed class CachedMethodAnnotation : IAnnotation<IMethod>
{
    public CachingOptions Options { get; }

    public CachedMethodAnnotation( CachingOptions options )
    {
        this.Options = options;
    }
}