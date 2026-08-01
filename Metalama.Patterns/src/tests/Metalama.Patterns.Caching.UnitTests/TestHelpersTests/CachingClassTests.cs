// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

            // Suspend the method body on a signal that this test controls. The assertions below concern the state of
            // the object before the body has run, and that state is only observable in a deterministic manner while
            // the body is held at a suspension point.
            cachingClass.SuspendAsyncMethods();

            var valueTask = cachingClass.GetValueAsync();
            var called = cachingClass.Reset();
            Assert.False( called, "The caching method was called before awaiting the first value." );
            Assert.False( valueTask.IsCompleted, "The cached method completed before its body was allowed to run." );

            cachingClass.ResumeAsyncMethods();
            await valueTask;
            called = cachingClass.Reset();
            Assert.True( called, "The method was not called when awaiting the first value." );

            called = cachingClass.Reset();
            Assert.False( called, "It is indicated the method has been called after the flag was reset." );
        }

        /// <summary>
        /// Verifies that <see cref="CachingClass{T}.SuspendAsyncMethods"/> and
        /// <see cref="CachingClass{T}.ResumeAsyncMethods"/> reject the calls that do not match the current state,
        /// so that a test cannot silently assert on a suspension that is not in effect.
        /// </summary>
        [Fact]
        public void TestAsyncSuspensionGuards()
        {
            var cachingClass = new CachingClass();

            Assert.Throws<InvalidOperationException>( () => cachingClass.ResumeAsyncMethods() );

            cachingClass.SuspendAsyncMethods();
            Assert.Throws<InvalidOperationException>( () => cachingClass.SuspendAsyncMethods() );

            cachingClass.ResumeAsyncMethods();
            Assert.Throws<InvalidOperationException>( () => cachingClass.ResumeAsyncMethods() );
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