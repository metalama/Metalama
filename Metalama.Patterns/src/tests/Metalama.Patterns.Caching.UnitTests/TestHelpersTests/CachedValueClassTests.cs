// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.TestHelpers;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.TestHelpersTests
{
    public sealed class CachedValueClassTests
    {
        [Fact]
        public void TestEquality()
        {
            var value0A = new CachedValueClass( 0 );
            var value0B = new CachedValueClass( 0 );
            var value1 = new CachedValueClass( 1 );

            AssertEx.Equal( value0A.GetHashCode(), value0B.GetHashCode(), "Two different instances with the same IDs have different hash codes." );
            AssertEx.Equal( value0A, value0B, "Two different instances with the same IDs are not considered equal." );
            AssertEx.NotEqual( value0A, value1, "Two different instances with different IDs are considered equal." );
        }

        [Fact]
        public void TestInequality()
        {
            var value0 = new CachedValueClass( 0 );
            var value1 = new CachedValueClass( 1 );

            AssertEx.NotEqual( value0, value1, "Two different instances with different IDs are considered equal." );
        }

        [Fact]
        public void TestEqualHashCodes()
        {
            var value0A = new CachedValueClass( 0 );
            var value0B = new CachedValueClass( 0 );

            AssertEx.Equal( value0A.GetHashCode(), value0B.GetHashCode(), "Two different instances with the same IDs have different hash codes." );
        }

        [Fact]
        public void TestUnequalHashCodes()
        {
            var value0 = new CachedValueClass( 0 );
            var value1 = new CachedValueClass( 1 );

            AssertEx.NotEqual( value0.GetHashCode(), value1.GetHashCode(), "Two different instances with different IDs have the same hash codes." );
        }
    }
}