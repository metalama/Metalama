// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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