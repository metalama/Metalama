// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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