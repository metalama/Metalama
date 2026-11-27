// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;

// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Global

namespace Metalama.Patterns.Caching.Tests
{
    public sealed class DependencyPropagationTests : BaseCachingTests
    {
        private const string _profileNamePrefix = "Caching.Tests.DependencyPropagationTests_";

        #region TestDependencyPropagation

        private const string _testDependencyPropagationProfileName = _profileNamePrefix + "DependencyPropagation";

        [CachingConfiguration( ProfileName = _testDependencyPropagationProfileName )]
        private sealed class TestDependencyPropagationCachingClass
        {
            public bool WasGetValueCalled { get; set; }

            public bool WasGetValueIntermediateCalled { get; set; }

            public bool WasGetValueDependencyCalled { get; set; }

            [Cache]
            public CachedValueClass GetValue()
            {
                this.WasGetValueCalled = true;

                return this.GetValueIntermediate();
            }

            [Cache]
            public CachedValueClass GetValueIntermediate()
            {
                this.WasGetValueIntermediateCalled = true;

                return this.GetValueDependency();
            }

            [Cache]
            public CachedValueClass GetValueDependency()
            {
                this.WasGetValueDependencyCalled = true;

                return new CachedValueClass();
            }
        }

        [Fact]
        public void TestDependencyPropagation()
        {
            using var context = this.InitializeTestWithTestingBackend( _testDependencyPropagationProfileName );

            var cachingClass = new TestDependencyPropagationCachingClass();

            cachingClass.GetValue();

            cachingClass.WasGetValueCalled = false;
            cachingClass.WasGetValueIntermediateCalled = false;
            cachingClass.WasGetValueDependencyCalled = false;

            CachingService.Default.Invalidate( cachingClass.GetValueDependency );

            cachingClass.GetValue();

            Assert.True( cachingClass.WasGetValueCalled, "The outer method was not invalidated." );
            Assert.True( cachingClass.WasGetValueIntermediateCalled, "The intermediate method was not invalidated." );
            Assert.True( cachingClass.WasGetValueDependencyCalled, "The inner method was not invalidated." );
        }

        #endregion

        #region TestDependencyPropagationAsync

        private const string _testDependencyPropagationAsyncProfileName = _profileNamePrefix + "DependencyPropagationAsync";

        [CachingConfiguration( ProfileName = _testDependencyPropagationAsyncProfileName )]
        private sealed class TestDependencyPropagationAsyncCachingClass
        {
            public bool WasGetValueCalled { get; set; }

            public bool WasGetValueIntermediateCalled { get; set; }

            public bool WasGetValueDependencyCalled { get; set; }

            [Cache]
            public async Task<CachedValueClass> GetValueAsync()
            {
                this.WasGetValueCalled = true;
                await Task.Yield();

                return await this.GetValueIntermediateAsync();
            }

            [Cache]
            public async Task<CachedValueClass> GetValueIntermediateAsync()
            {
                this.WasGetValueIntermediateCalled = true;
                await Task.Yield();

                return await this.GetValueDependencyAsync();
            }

            [Cache]
            public async Task<CachedValueClass> GetValueDependencyAsync()
            {
                this.WasGetValueDependencyCalled = true;
                await Task.Yield();

                return new CachedValueClass();
            }
        }

        [Fact]
        public async Task TestDependencyPropagationAsync()
        {
            await using var context = this.InitializeTestWithTestingBackend( _testDependencyPropagationAsyncProfileName );

            var cachingClass = new TestDependencyPropagationAsyncCachingClass();

            await cachingClass.GetValueAsync();

            cachingClass.WasGetValueCalled = false;
            cachingClass.WasGetValueIntermediateCalled = false;
            cachingClass.WasGetValueDependencyCalled = false;

            await CachingService.Default.InvalidateAsync( cachingClass.GetValueDependencyAsync, TestContext.Current.CancellationToken );

            await cachingClass.GetValueAsync();

            Assert.True( cachingClass.WasGetValueCalled, "The outer method was not invalidated." );
            Assert.True( cachingClass.WasGetValueIntermediateCalled, "The intermediate method was not invalidated." );
            Assert.True( cachingClass.WasGetValueDependencyCalled, "The inner method was not invalidated." );
        }

        #endregion

        #region TestDisposedContextAsync

        private const string _testDisposedContextAsyncProfileName = _profileNamePrefix + "DisposedContextAsync";

        // [Porting] Won't fix, can't be certain of original intent.
#pragma warning disable CA1001
        [CachingConfiguration( ProfileName = _testDisposedContextAsyncProfileName )]
        private sealed class TestDisposedContextAsyncCachingClass
#pragma warning restore CA1001
        {
            public bool WasGetValueCalled { get; set; }

            public bool WasGetValueDependencyCalled { get; set; }

            private volatile Task? _syncTask;

            [Cache]
            public async Task<CachedValueClass> GetValueAsync()
            {
                this.WasGetValueCalled = true;
                await Task.Yield();
                this._syncTask = new Task( () => { } );
                var dependencyTask = await this.GetValueIntermediateAsync();
                this._syncTask.Start();

                return await dependencyTask;
            }

            public async Task<Task<CachedValueClass>> GetValueIntermediateAsync()
            {
                using ( CachingContext.OpenCacheContext( "k" ) )
                {
                    await Task.Yield();

                    return Task.Run( async () => await this.GetValueDependencyAsync() );
                }
            }

            [Cache]
            public async Task<CachedValueClass> GetValueDependencyAsync()
            {
                this.WasGetValueDependencyCalled = true;
                await this._syncTask!;

                return new CachedValueClass();
            }
        }

        [Fact]
        public async Task TestDisposedContextAsync()
        {
            await using var context = this.InitializeTestWithTestingBackend( _testDisposedContextAsyncProfileName );

            // This test is trying not-so-nicely replicate previously crashing scenario with context being disposed before it's child context.
            // The real-world scenario is probably much less strange and much more complicated.

            var cachingClass = new TestDisposedContextAsyncCachingClass();

            await cachingClass.GetValueAsync();

            cachingClass.WasGetValueCalled = false;
            cachingClass.WasGetValueDependencyCalled = false;

            await CachingService.Default.InvalidateAsync( cachingClass.GetValueDependencyAsync, TestContext.Current.CancellationToken );

            await cachingClass.GetValueAsync();

            Assert.True( cachingClass.WasGetValueCalled, "The outer method was not invalidated." );
            Assert.True( cachingClass.WasGetValueDependencyCalled, "The inner method was not invalidated." );
        }

        #endregion

        #region TestSuspendedDependencyPropagation

        private const string _testSuspendedDependencyPropagationProfileName = _profileNamePrefix + "SuspendedDependencyPropagation";

        [CachingConfiguration( ProfileName = _testSuspendedDependencyPropagationProfileName )]
        private sealed class TestSuspendedDependencyPropagationCachingClass
        {
            public bool WasGetValueCalled { get; set; }

            public bool WasGetValueIntermediateCalled { get; set; }

            public bool WasGetValueDependencyCalled { get; set; }

            [Cache]
            public CachedValueClass GetValue()
            {
                this.WasGetValueCalled = true;

                return this.GetValueIntermediate();
            }

            [Cache]
            public CachedValueClass GetValueIntermediate()
            {
                this.WasGetValueIntermediateCalled = true;

                using ( CachingService.Default.SuspendDependencyPropagation() )
                {
                    return this.GetValueDependency();
                }
            }

            [Cache]
            public CachedValueClass GetValueDependency()
            {
                this.WasGetValueDependencyCalled = true;

                return new CachedValueClass();
            }
        }

        [Fact]
        public void TestSuspendedDependencyPropagation()
        {
            using var context = this.InitializeTestWithTestingBackend( _testSuspendedDependencyPropagationProfileName );

            var cachingClass = new TestSuspendedDependencyPropagationCachingClass();

            cachingClass.GetValue();

            cachingClass.WasGetValueCalled = false;
            cachingClass.WasGetValueIntermediateCalled = false;
            cachingClass.WasGetValueDependencyCalled = false;

            CachingService.Default.Invalidate( cachingClass.GetValueDependency );

            cachingClass.GetValue();

            Assert.False( cachingClass.WasGetValueCalled, "The outer method was invalidated." );
            Assert.False( cachingClass.WasGetValueIntermediateCalled, "The intermediate method was invalidated." );
            Assert.False( cachingClass.WasGetValueDependencyCalled, "The dependency method was called." );

            cachingClass.GetValueDependency();

            Assert.True( cachingClass.WasGetValueDependencyCalled, "The intermediate method was not invalidated." );
        }

        #endregion

        #region TestSuspendedDependencyPropagationAsync

        private const string _testSuspendedDependencyPropagationAsyncProfileName = _profileNamePrefix + "SuspendedDependencyPropagationAsync";

        [CachingConfiguration( ProfileName = _testSuspendedDependencyPropagationAsyncProfileName )]
        private sealed class TestSuspendedDependencyPropagationAsyncCachingClass
        {
            public bool WasGetValueCalled { get; set; }

            public bool WasGetValueIntermediateCalled { get; set; }

            public bool WasGetValueDependencyCalled { get; set; }

            [Cache]
            public async Task<CachedValueClass> GetValueAsync()
            {
                this.WasGetValueCalled = true;
                await Task.Yield();

                return await this.GetValueIntermediateAsync();
            }

            [Cache]
            public async Task<CachedValueClass> GetValueIntermediateAsync()
            {
                this.WasGetValueIntermediateCalled = true;
                await Task.Yield();

                using ( CachingService.Default.SuspendDependencyPropagation() )
                {
                    return await this.GetValueDependencyAsync();
                }
            }

            [Cache]
            public async Task<CachedValueClass> GetValueDependencyAsync()
            {
                this.WasGetValueDependencyCalled = true;
                await Task.Yield();

                return new CachedValueClass();
            }
        }

        [Fact]
        public async Task TestSuspendedDependencyPropagationAsync()
        {
            await using var context = this.InitializeTestWithTestingBackend( _testSuspendedDependencyPropagationAsyncProfileName );

            var cachingClass = new TestSuspendedDependencyPropagationAsyncCachingClass();

            await cachingClass.GetValueAsync();

            cachingClass.WasGetValueCalled = false;
            cachingClass.WasGetValueIntermediateCalled = false;
            cachingClass.WasGetValueDependencyCalled = false;

            await CachingService.Default.InvalidateAsync( cachingClass.GetValueDependencyAsync, TestContext.Current.CancellationToken );

            await cachingClass.GetValueAsync();

            Assert.False( cachingClass.WasGetValueCalled, "The outer method was invalidated." );
            Assert.False( cachingClass.WasGetValueIntermediateCalled, "The intermediate method was invalidated." );
            Assert.False( cachingClass.WasGetValueDependencyCalled, "The dependency method was called." );

            await cachingClass.GetValueDependencyAsync();

            Assert.True( cachingClass.WasGetValueDependencyCalled, "The intermediate method was not invalidated." );
        }

        #endregion

        #region TestDependencyPropagationOnCacheHitAfterEviction

        private const string _testDependencyPropagationOnCacheHitAfterEvictionProfileName =
            _profileNamePrefix + "DependencyPropagationOnCacheHitAfterEviction";

        /// <summary>
        /// A chain of three cached methods. The outer method invokes <see cref="AfterIntermediateCall"/> after it gets the
        /// value of the intermediate method, so that a test can evict the intermediate item before the outer item is stored.
        /// </summary>
        [CachingConfiguration( ProfileName = _testDependencyPropagationOnCacheHitAfterEvictionProfileName )]
        private sealed class TestDependencyPropagationOnCacheHitAfterEvictionCachingClass
        {
            /// <summary>
            /// Gets or sets an action that the outer method invokes after it gets the value of the intermediate method.
            /// </summary>
            public Action? AfterIntermediateCall { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the outer method has been evaluated.
            /// </summary>
            public bool WasGetValueCalled { get; set; }

            /// <summary>
            /// The outer cached method.
            /// </summary>
            [Cache]
            public CachedValueClass GetValue()
            {
                this.WasGetValueCalled = true;

                var value = this.GetValueIntermediate();

                this.AfterIntermediateCall?.Invoke();

                return value;
            }

            /// <summary>
            /// The intermediate cached method.
            /// </summary>
            [Cache]
            public CachedValueClass GetValueIntermediate() => this.GetValueDependency();

            /// <summary>
            /// The inner cached method, on which the intermediate method depends.
            /// </summary>
            [Cache]
            public CachedValueClass GetValueDependency()
            {
                this.WasGetValueDependencyCalled = true;

                return new CachedValueClass();
            }

            /// <summary>
            /// Gets or sets a value indicating whether the inner method has been evaluated.
            /// </summary>
            public bool WasGetValueDependencyCalled { get; set; }
        }

        /// <summary>
        /// Verifies that the invalidation of the inner method invalidates the outer method when the outer method got a cache
        /// hit on the intermediate method and the intermediate item was evicted before the outer item was stored.
        /// </summary>
        /// <remarks>
        /// When the intermediate item is evicted, no item depends on it yet, so the backend keeps no registration for its
        /// dependencies. The outer item is invalidated only if the cache hit added the dependencies of the intermediate item
        /// to the outer item.
        /// </remarks>
        [Fact]
        public void TestDependencyPropagationOnCacheHitAfterEviction()
        {
            using var context = this.InitializeTestWithTestingBackend( _testDependencyPropagationOnCacheHitAfterEvictionProfileName );

            var cachingClass = new TestDependencyPropagationOnCacheHitAfterEvictionCachingClass();

            cachingClass.GetValueIntermediate();

            var intermediateKey = context.Backend.LastCachedKey;
            Assert.NotNull( intermediateKey );

            cachingClass.AfterIntermediateCall = () => context.Backend.RemoveItem( intermediateKey );

            cachingClass.GetValue();

            Assert.False( context.Backend.ContainsItem( intermediateKey ), "Precondition: the intermediate item was not evicted." );

            cachingClass.AfterIntermediateCall = null;
            cachingClass.WasGetValueCalled = false;

            CachingService.Default.Invalidate( cachingClass.GetValueDependency );

            cachingClass.GetValue();

            Assert.True( cachingClass.WasGetValueCalled, "The outer method was not invalidated." );
        }

        #endregion

        #region TestDependencyPropagationOnCacheHitAfterEvictionAsync

        private const string _testDependencyPropagationOnCacheHitAfterEvictionAsyncProfileName =
            _profileNamePrefix + "DependencyPropagationOnCacheHitAfterEvictionAsync";

        /// <summary>
        /// The asynchronous version of <see cref="TestDependencyPropagationOnCacheHitAfterEvictionCachingClass"/>.
        /// </summary>
        [CachingConfiguration( ProfileName = _testDependencyPropagationOnCacheHitAfterEvictionAsyncProfileName )]
        private sealed class TestDependencyPropagationOnCacheHitAfterEvictionAsyncCachingClass
        {
            /// <summary>
            /// Gets or sets a function that the outer method invokes after it gets the value of the intermediate method.
            /// </summary>
            public Func<Task>? AfterIntermediateCall { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the outer method has been evaluated.
            /// </summary>
            public bool WasGetValueCalled { get; set; }

            /// <summary>
            /// The outer cached method.
            /// </summary>
            [Cache]
            public async Task<CachedValueClass> GetValueAsync()
            {
                this.WasGetValueCalled = true;
                await Task.Yield();

                var value = await this.GetValueIntermediateAsync();

                if ( this.AfterIntermediateCall != null )
                {
                    await this.AfterIntermediateCall();
                }

                return value;
            }

            /// <summary>
            /// The intermediate cached method.
            /// </summary>
            [Cache]
            public async Task<CachedValueClass> GetValueIntermediateAsync()
            {
                await Task.Yield();

                return await this.GetValueDependencyAsync();
            }

            /// <summary>
            /// The inner cached method, on which the intermediate method depends.
            /// </summary>
            [Cache]
            public async Task<CachedValueClass> GetValueDependencyAsync()
            {
                this.WasGetValueDependencyCalled = true;
                await Task.Yield();

                return new CachedValueClass();
            }

            /// <summary>
            /// Gets or sets a value indicating whether the inner method has been evaluated.
            /// </summary>
            public bool WasGetValueDependencyCalled { get; set; }
        }

        /// <summary>
        /// The asynchronous version of <see cref="TestDependencyPropagationOnCacheHitAfterEviction"/>.
        /// </summary>
        [Fact]
        public async Task TestDependencyPropagationOnCacheHitAfterEvictionAsync()
        {
            await using var context = this.InitializeTestWithTestingBackend( _testDependencyPropagationOnCacheHitAfterEvictionAsyncProfileName );

            var cancellationToken = TestContext.Current.CancellationToken;
            var cachingClass = new TestDependencyPropagationOnCacheHitAfterEvictionAsyncCachingClass();

            await cachingClass.GetValueIntermediateAsync();

            var intermediateKey = context.Backend.LastCachedKey;
            Assert.NotNull( intermediateKey );

            cachingClass.AfterIntermediateCall = () => context.Backend.RemoveItemAsync( intermediateKey, cancellationToken ).AsTask();

            await cachingClass.GetValueAsync();

            Assert.False(
                await context.Backend.ContainsItemAsync( intermediateKey, cancellationToken ),
                "Precondition: the intermediate item was not evicted." );

            cachingClass.AfterIntermediateCall = null;
            cachingClass.WasGetValueCalled = false;

            await CachingService.Default.InvalidateAsync( cachingClass.GetValueDependencyAsync, cancellationToken );

            await cachingClass.GetValueAsync();

            Assert.True( cachingClass.WasGetValueCalled, "The outer method was not invalidated." );
        }

        #endregion

        public DependencyPropagationTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }
    }
}