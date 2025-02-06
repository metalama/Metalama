// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests;

public sealed class AspectlessTests : BaseCachingTests
{
    public AspectlessTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }

    [Fact]
    public void TestCacheHit()
    {
        using ( this.InitializeTest( nameof(AspectlessTests) ) )
        {
            var o = new C();

            var value1 = o.Get();
            var value2 = o.Get();

            Assert.Equal( value1, value2 );
        }
    }

    private sealed class C
    {
        private int _invocations;

        public int Get()
        {
            var cachedMethod = CachedMethodMetadata.ForCallingMethod();

            Assert.Equal( nameof(this.Get), cachedMethod.Method.Name );

            return CachingService.Default.GetFromCacheOrExecute<int>( cachedMethod, this, [], ( instance, args ) => this._invocations++ );
        }
    }
}