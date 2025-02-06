// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.TestHelpers;
using Metalama.Patterns.Caching.Tests.Assets;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests
{
    public sealed class Issue20774Tests : BaseCachingTests
    {
        [Fact]
        public void InvalidateBeforeCachedClassHasBeenTouched()
        {
            using var context = this.InitializeTest( nameof(Issue20774Tests) );

            // This shouldn't fail, even though the CachedClass type hasn't been touched yet.
            new InvalidatingClass().Invalidate();
        }

        public Issue20774Tests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }
    }
}