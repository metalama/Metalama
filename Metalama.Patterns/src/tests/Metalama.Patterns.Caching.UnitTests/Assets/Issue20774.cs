// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;

namespace Metalama.Patterns.Caching.Tests.Assets
{
    public class CachedClass
    {
        [Cache( IgnoreThisParameter = true )]
        public int GetValue()
        {
            return 0;
        }
    }

    public class InvalidatingClass
    {
        [InvalidateCache( typeof(CachedClass), "GetValue" )]
        public void Invalidate() { }
    }
}