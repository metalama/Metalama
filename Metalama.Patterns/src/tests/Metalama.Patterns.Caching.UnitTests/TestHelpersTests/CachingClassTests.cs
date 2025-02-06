// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.TestHelpers;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.TestHelpersTests
{
    public sealed class CachingClassTests
    {
        [Fact]
        public void TestReset()
        {
            var cachingClass = new CachingClass();

            var called = cachingClass.Reset();
            Assert.False( called, "It is indicated the method has been called on a fresh instance." );

            cachingClass.GetValue();
            called = cachingClass.Reset();
            Assert.True( called, "It is indicated the method has not been called after we have called it." );

            called = cachingClass.Reset();
            Assert.False( called, "It is indicated the method has been called after the flag was reset." );
        }

        [Fact]
        public async Task TestAsyncReset()
        {
            var cachingClass = new CachingClass();

            var valueTask = cachingClass.GetValueAsync();
            var called = cachingClass.Reset();
            Assert.False( called, "The caching method was called before awaiting the first value." );
            await valueTask;
            called = cachingClass.Reset();
            Assert.True( called, "The method was not called when awaiting the first value." );

            called = cachingClass.Reset();
            Assert.False( called, "It is indicated the method has been called after the flag was reset." );
        }

        [Fact]
        public void TestCounter()
        {
            var cachingClass = new CachingClass();

            var value0 = cachingClass.GetValue();
            cachingClass.Reset();
            var value1 = cachingClass.GetValue();

            AssertEx.NotEqual( value0, value1, "The method returned the same objects twice." );
        }

        [Fact]
        public async Task TestAsyncCounter()
        {
            var cachingClass = new CachingClass();

            var valueTask = cachingClass.GetValueAsync();
            var value0 = await valueTask;
            cachingClass.Reset();

            valueTask = cachingClass.GetValueAsync();
            var value1 = await valueTask;

            AssertEx.NotEqual( value0, value1, "The method returned the same objects twice." );
        }
    }
}