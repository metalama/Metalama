// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests
{
    public sealed class ObjectDependenciesTests : BaseCachingTests
    {
        private const string _profileNamePrefix = "Caching.Tests.ObjectDependenciesTests_";

        #region TestOneDependency

        private const string _testOneDependencyProfileName = _profileNamePrefix + "TestOneDependency";

        [CachingConfiguration( ProfileName = _testOneDependencyProfileName )]
        private sealed class TestOneDependencyCachingClass : CachingClass
        {
            [Cache]
            public override CachedValueClass GetValueAsDependency()
            {
                return base.GetValueAsDependency();
            }
        }

        [Fact]
        public void TestOneDependency()
        {
            using var context = this.InitializeTest( _testOneDependencyProfileName );

            var cachingClass = new TestOneDependencyCachingClass();
            var currentId = 0;

            var value1 = cachingClass.GetValueAsDependency();
            var called = cachingClass.Reset();
            Assert.True( called, "The method was not called when the cache should be empty." );
            AssertEx.Equal( currentId, value1.Id, "The first given value has unexpected ID." );

            var value2 = cachingClass.GetValueAsDependency();
            called = cachingClass.Reset();
            Assert.False( called, "The method was called when its first return value should be cached." );

            AssertEx.Equal(
                value1,
                value2,
                "The first value, which should be returned from the cache, is not the same as the one which should have been cached." );

            CachingService.Default.InvalidateObject( value1 );

            ++currentId;
            var value3 = cachingClass.GetValueAsDependency();
            called = cachingClass.Reset();
            Assert.True( called, "The method was not called when the cache item should be invalidated." );
            AssertEx.Equal( currentId, value3.Id, "The second given value has unexpected ID." );

            var value4 = cachingClass.GetValueAsDependency();
            called = cachingClass.Reset();
            Assert.False( called, "The method was called when its second return value should be cached." );

            AssertEx.Equal(
                value3,
                value4,
                "The second value, which should be returned from the cache, is not the same as the one which should have been cached." );
        }

        #endregion TestOneDependency

        #region TestOneDependencyAsync

        private const string _testOneDependencyAsyncProfileName = _profileNamePrefix + "TestOneDependencyAsync";

        [CachingConfiguration( ProfileName = _testOneDependencyAsyncProfileName )]
        private sealed class TestOneDependencyAsyncCachingClass : CachingClass
        {
            [Cache]
            public override async Task<CachedValueClass> GetValueAsDependencyAsync()
            {
                return await base.GetValueAsDependencyAsync();
            }
        }

        [Fact]
        public async Task TestOneDependencyAsync()
        {
            await using var context = this.InitializeTest( _testOneDependencyAsyncProfileName );

            var cachingClass = new TestOneDependencyAsyncCachingClass();
            var currentId = 0;

            var value1 = await cachingClass.GetValueAsDependencyAsync();
            var called = cachingClass.Reset();
            Assert.True( called, "The method was not called when the cache should be empty." );
            AssertEx.Equal( currentId, value1.Id, "The first given value has unexpected ID." );

            var value2 = await cachingClass.GetValueAsDependencyAsync();
            called = cachingClass.Reset();
            Assert.False( called, "The method was called when its first return value should be cached." );

            AssertEx.Equal(
                value1,
                value2,
                "The first value, which should be returned from the cache, is not the same as the one which should have been cached." );

            await CachingService.Default.InvalidateObjectAsync( value1 );

            ++currentId;
            var value3 = await cachingClass.GetValueAsDependencyAsync();
            called = cachingClass.Reset();
            Assert.True( called, "The method was not called when the cache item should be invalidated." );
            AssertEx.Equal( currentId, value3.Id, "The second given value has unexpected ID." );

            var value4 = await cachingClass.GetValueAsDependencyAsync();
            called = cachingClass.Reset();
            Assert.False( called, "The method was called when its second return value should be cached." );

            AssertEx.Equal(
                value3,
                value4,
                "The second value, which should be returned from the cache, is not the same as the one which should have been cached." );
        }

        #endregion TestOneDependencyAsync

        #region TestNestedDependencies

        private const string _testNestedDependenciesProfileName = _profileNamePrefix + "TestNestedDependencies";

        private sealed class TestNestedDependenciesCachedValueClass1 : CachedValueClass;

        private sealed class TestNestedDependenciesCachedValueClass2 : CachedValueClass;

        [CachingConfiguration( ProfileName = _testNestedDependenciesProfileName )]
        private sealed class TestNestedDependenciesCachingClass1 : CachingClass<TestNestedDependenciesCachedValueClass1>
        {
            [Cache]
            public override TestNestedDependenciesCachedValueClass1 GetValueAsDependency()
            {
                return base.GetValueAsDependency();
            }
        }

        [CachingConfiguration( ProfileName = _testNestedDependenciesProfileName )]
        private sealed class TestNestedDependenciesCachingClass2 : CachingClass<TestNestedDependenciesCachedValueClass2>
        {
            public TestNestedDependenciesCachingClass1 Class1 { get; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            // ReSharper disable once MemberCanBePrivate.Local
            public TestNestedDependenciesCachedValueClass1 Value1 { get; private set; } = null!;

            public TestNestedDependenciesCachingClass2()
            {
                this.Class1 = new TestNestedDependenciesCachingClass1();
            }

            [Cache]
            public override TestNestedDependenciesCachedValueClass2 GetValueAsDependency()
            {
                this.Value1 = this.Class1.GetValueAsDependency();

                return base.GetValueAsDependency();
            }
        }

        [Fact]
        public void TestNestedDependencies()
        {
            using var context = this.InitializeTest( _testNestedDependenciesProfileName );

            var cachingClass2 = new TestNestedDependenciesCachingClass2();
            var cachingClass1 = cachingClass2.Class1;

            CachedValueClass value1 = cachingClass2.GetValueAsDependency();
            cachingClass1.Reset();
            cachingClass2.Reset();

            CachingService.Default.InvalidateObject( value1 );

            cachingClass2.GetValueAsDependency();
            var called = cachingClass1.Reset();
            Assert.True( called, "The method result did not get invalidated by the automatic dependency." );
        }

        #endregion TestNestedDependencies

        #region TestNestedDependenciesAsync

        private const string _testNestedDependenciesAsyncProfileName = _profileNamePrefix + "TestNestedDependenciesAsync";

        private sealed class TestNestedDependenciesAsyncCachedValueClass1 : CachedValueClass;

        private sealed class TestNestedDependenciesAsyncCachedValueClass2 : CachedValueClass;

        [CachingConfiguration( ProfileName = _testNestedDependenciesAsyncProfileName )]
        private sealed class TestNestedDependenciesAsyncCachingClass1 : CachingClass<TestNestedDependenciesAsyncCachedValueClass1>
        {
            [Cache]
            public override async Task<TestNestedDependenciesAsyncCachedValueClass1> GetValueAsDependencyAsync()
            {
                return await base.GetValueAsDependencyAsync();
            }
        }

        [CachingConfiguration( ProfileName = _testNestedDependenciesAsyncProfileName )]
        private sealed class TestNestedDependenciesAsyncCachingClass2 : CachingClass<TestNestedDependenciesAsyncCachedValueClass2>
        {
            public TestNestedDependenciesAsyncCachingClass1 Class1 { get; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            // ReSharper disable once MemberCanBePrivate.Local
            public TestNestedDependenciesAsyncCachedValueClass1 Value1 { get; private set; } = null!;

            public TestNestedDependenciesAsyncCachingClass2()
            {
                this.Class1 = new TestNestedDependenciesAsyncCachingClass1();
            }

            [Cache]
            public override async Task<TestNestedDependenciesAsyncCachedValueClass2> GetValueAsDependencyAsync()
            {
                this.Value1 = await this.Class1.GetValueAsDependencyAsync();

                return await base.GetValueAsDependencyAsync();
            }
        }

        [Fact]
        public async Task TestNestedDependenciesAsync()
        {
            await using var context = this.InitializeTest( _testNestedDependenciesAsyncProfileName );

            var cachingClass2 = new TestNestedDependenciesAsyncCachingClass2();
            var cachingClass1 = cachingClass2.Class1;

            CachedValueClass value1 = await cachingClass2.GetValueAsDependencyAsync();
            cachingClass1.Reset();
            cachingClass2.Reset();

            await CachingService.Default.InvalidateObjectAsync( value1 );

            await cachingClass2.GetValueAsDependencyAsync();
            var called = cachingClass1.Reset();
            Assert.True( called, "The method result did not get invalidated by the automatic dependency." );
        }

        #endregion TestNestedDependenciesAsync

        #region TestNestedDependenciesWithEnumerable

        private const string _testNestedDependenciesWithEnumerableProfileName = _profileNamePrefix + "TestNestedDependenciesWithEnumerable";

        private sealed class TestNestedDependenciesWithEnumerableCachedValueClass : CachedValueClass;

        private sealed class TestNestedDependenciesWithEnumerableCachingClass1 : CachingClass<TestNestedDependenciesWithEnumerableCachedValueClass>;

        [CachingConfiguration( ProfileName = _testNestedDependenciesWithEnumerableProfileName )]
        private sealed class TestNestedDependenciesWithEnumerableCachingClass2
        {
            private readonly TestNestedDependenciesWithEnumerableCachingClass1 _class1 = new();

            private int _class1CallCount;

            [Cache]
            public IEnumerable<TestNestedDependenciesWithEnumerableCachedValueClass> GetTwoValuesAsDependencies()
            {
                for ( var i = 0; i < 2; i++ )
                {
                    yield return this._class1.GetValueAsDependency();

                    if ( this._class1.Reset() )
                    {
                        ++this._class1CallCount;
                    }
                }
            }

            public int Reset()
            {
                var count = this._class1CallCount;
                this._class1CallCount = 0;

                return count;
            }
        }

        [Fact]
        public void TestNestedDependenciesWithEnumerable()
        {
            using var context = this.InitializeTest( _testNestedDependenciesWithEnumerableProfileName );

            var cachingClass2 = new TestNestedDependenciesWithEnumerableCachingClass2();

            IList<TestNestedDependenciesWithEnumerableCachedValueClass> value1 = cachingClass2.GetTwoValuesAsDependencies().ToList();
            var class1CallsCount = cachingClass2.Reset();
            AssertEx.Equal( value1.Count, class1CallsCount, "The method did not get called for the first time." );

            CachingService.Default.InvalidateObject( value1[0] );

            IList<TestNestedDependenciesWithEnumerableCachedValueClass> value2 = cachingClass2.GetTwoValuesAsDependencies().ToList();
            class1CallsCount = cachingClass2.Reset();
            AssertEx.Equal( value2.Count, class1CallsCount, "The method result did not get invalidated by the automatic dependency." );
        }

        #endregion TestNestedDependenciesWithEnumerable

        public ObjectDependenciesTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }
    }
}