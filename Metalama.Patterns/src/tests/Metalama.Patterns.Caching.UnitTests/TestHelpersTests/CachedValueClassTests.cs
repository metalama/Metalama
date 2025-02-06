// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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