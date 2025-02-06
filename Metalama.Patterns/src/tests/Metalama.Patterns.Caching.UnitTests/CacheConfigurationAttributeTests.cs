// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable UnusedType.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable MemberCanBeMadeStatic.Local
#pragma warning disable CA1822

namespace Metalama.Patterns.Caching.Tests
{
    public sealed class CacheConfigurationAttributeTests : BaseCachingTests
    {
        private const string _profileNamePrefix = "Caching.Tests.CacheConfigurationAttributeTests_";

        private const string _testCachingAttributeProfileName = _profileNamePrefix + "TestCachingAttribute";

        [CachingConfiguration( ProfileName = _testCachingAttributeProfileName )]
        private class BaseCachingClass
        {
            public sealed class InnerCachingClassInBase
            {
                [Cache]
                public object GetValueInnerBase()
                {
                    return null!;
                }

                [Cache]
                public async Task<object> GetValueInnerBaseAsync()
                {
                    await Task.Yield();

                    return null!;
                }
            }

            [Cache]
            public object GetValueBase()
            {
                return null!;
            }

            [Cache]
            public async Task<object> GetValueBaseAsync()
            {
                await Task.Yield();

                return null!;
            }
        }

        private sealed class ChildCachingClass : BaseCachingClass
        {
            public sealed class InnerCachingClassInChild
            {
                [Cache]
                public object GetValueInnerChild()
                {
                    return null!;
                }

                [Cache]
                public async Task<object> GetValueInnerChildAsync()
                {
                    await Task.Yield();

                    return null!;
                }
            }

            [Cache]
            public object GetValueChild()
            {
                return null!;
            }

            [Cache]
            public async Task<object> GetValueChildAsync()
            {
                await Task.Yield();

                return null!;
            }
        }

        private void DoCachingAttributeTest( Func<object> getValueAction, bool defaultProfile )
        {
            using var context =
                this.InitializeTestWithTestingBackend( _testCachingAttributeProfileName );

            var backend = context.Backend;

            Assert.Null( backend.LastCachedKey );
            Assert.Null( backend.LastCachedItem );

            getValueAction.Invoke();

            Assert.NotNull( backend.LastCachedKey );
            Assert.NotNull( backend.LastCachedItem );

            Assert.Equal(
                defaultProfile ? CachingProfile.DefaultName : _testCachingAttributeProfileName,
                backend.LastCachedItem!.Configuration!.ProfileName );
        }

        private async Task DoCachingAttributeTestAsync( Func<Task<object>> getValueAction, bool defaultProfile )
        {
            await using var context =
                this.InitializeTestWithTestingBackend( _testCachingAttributeProfileName );

            var backend = context.Backend;

            Assert.Null( backend.LastCachedKey );
            Assert.Null( backend.LastCachedItem );

            await getValueAction.Invoke();

            Assert.NotNull( backend.LastCachedKey );
            Assert.NotNull( backend.LastCachedItem );

            Assert.Equal(
                defaultProfile ? CachingProfile.DefaultName : _testCachingAttributeProfileName,
                backend.LastCachedItem!.Configuration!.ProfileName );
        }

        [Fact]
        public void TestCachingAttributeBase()
        {
            var cachingClass = new ChildCachingClass();
            this.DoCachingAttributeTest( cachingClass.GetValueBase, false );
        }

        [Fact]
        public async Task TestCachingAttributeBaseAsync()
        {
            var cachingClass = new ChildCachingClass();
            await this.DoCachingAttributeTestAsync( cachingClass.GetValueBaseAsync, false );
        }

        [Fact]
        public void TestCachingAttributeChild()
        {
            var cachingClass = new ChildCachingClass();
            this.DoCachingAttributeTest( cachingClass.GetValueChild, false );
        }

        [Fact]
        public async Task TestCachingAttributeChildAsync()
        {
            var cachingClass = new ChildCachingClass();
            await this.DoCachingAttributeTestAsync( cachingClass.GetValueChildAsync, false );
        }

        public CacheConfigurationAttributeTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }
    }
}