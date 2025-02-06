// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Caching.TestHelpers
{
    [Serializable]
    public class CachedValueChildClass : CachedValueClass
    {
        public CachedValueChildClass() { }

        public CachedValueChildClass( int id ) : base( id ) { }
    }
}