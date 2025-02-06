// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;
using Xunit.Abstractions;

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

            await CachingService.Default.InvalidateAsync( cachingClass.GetValueDependencyAsync );

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

            await CachingService.Default.InvalidateAsync( cachingClass.GetValueDependencyAsync );

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

            await CachingService.Default.InvalidateAsync( cachingClass.GetValueDependencyAsync );

            await cachingClass.GetValueAsync();

            Assert.False( cachingClass.WasGetValueCalled, "The outer method was invalidated." );
            Assert.False( cachingClass.WasGetValueIntermediateCalled, "The intermediate method was invalidated." );
            Assert.False( cachingClass.WasGetValueDependencyCalled, "The dependency method was called." );

            await cachingClass.GetValueDependencyAsync();

            Assert.True( cachingClass.WasGetValueDependencyCalled, "The intermediate method was not invalidated." );
        }

        #endregion

        public DependencyPropagationTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }
    }
}