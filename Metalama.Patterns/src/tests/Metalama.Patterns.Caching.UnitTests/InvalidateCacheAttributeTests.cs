// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.TestHelpers;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal
// #pragma warning disable SA1203
#pragma warning disable CA1822

namespace Metalama.Patterns.Caching.Tests
{
    public sealed class InvalidateCacheAttributeTests : InvalidationTestsBase
    {
        private const string _profileNamePrefix = "Caching.Tests.InvalidateCacheAttributeTests_";

        #region TestFromDifferentTypeIgnoringThisParameter

        private const string _testFromDifferentTypeIgnoringThisParameterProfileName = _profileNamePrefix + nameof(TestFromDifferentTypeIgnoringThisParameter);

        [CachingConfiguration( ProfileName = _testFromDifferentTypeIgnoringThisParameterProfileName )]
        private sealed class TestFromDifferentTypeIgnoringThisParameterCachingClass
        {
            private static int _counter;

            [Cache( IgnoreThisParameter = true )]
            public CachedValueClass GetValue()
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache( IgnoreThisParameter = true )]
            public CachedValueClass GetValue( int param1 )
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache( IgnoreThisParameter = true )]
            public CachedValueClass GetValue( int param1, CachedValueClass param2 )
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache( IgnoreThisParameter = true )]
            public CachedValueClass GetValue( int param1, CachedValueClass param2, int param3, [NotCacheKey] int param4 )
            {
                return new CachedValueClass( _counter++ );
            }
        }

        private sealed class TestFromDifferentTypeIgnoringThisParameterInvalidatingClass
        {
            [InvalidateCache(
                typeof(TestFromDifferentTypeIgnoringThisParameterCachingClass),
                nameof(TestFromDifferentTypeIgnoringThisParameterCachingClass.GetValue),
                AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeIgnoringThisParameterCachingClass),
                nameof(TestFromDifferentTypeIgnoringThisParameterCachingClass.GetValue),
                AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( int param1, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeIgnoringThisParameterCachingClass),
                nameof(TestFromDifferentTypeIgnoringThisParameterCachingClass.GetValue),
                AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( int param1, CachedValueChildClass param2, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeIgnoringThisParameterCachingClass),
                nameof(TestFromDifferentTypeIgnoringThisParameterCachingClass.GetValue),
                AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( int param1, CachedValueChildClass param2, int param3, int param4, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }
        }

        [Fact]
        public void TestFromDifferentTypeIgnoringThisParameter()
        {
            var cachingClass =
                new TestFromDifferentTypeIgnoringThisParameterCachingClass();

            var invalidatingClass =
                new TestFromDifferentTypeIgnoringThisParameterInvalidatingClass();

            var cachedValue0 = new CachedValueChildClass( 0 );
            var cachedValue2 = new CachedValueChildClass( 2 );

            var cachedMethods =
                new[]
                {
                    cachingClass.GetValue,
                    () => cachingClass.GetValue( 1 ),
                    () => cachingClass.GetValue( 1, cachedValue2 ),
                    () => cachingClass.GetValue( 1, cachedValue2, 3, 4 )
                };

            var invalidatingMethods =
                new[]
                {
                    () => invalidatingClass.Invalidate( cachedMethods[0] ),
                    () => invalidatingClass.Invalidate( 1, cachedMethods[1] ),
                    () => invalidatingClass.Invalidate( 1, cachedValue2, cachedMethods[2] ),
                    () => invalidatingClass.Invalidate( 1, cachedValue2, 3, 5, cachedMethods[3] )
                };

            this.DoInvalidateCacheAttributeTest(
                _testFromDifferentTypeIgnoringThisParameterProfileName,
                cachedMethods,
                invalidatingMethods,
                "Matching values test",
                true,
                true );

            invalidatingMethods =
            [
                () => invalidatingClass.Invalidate( cachedMethods[0] ),
                () => invalidatingClass.Invalidate( 0, cachedMethods[1] ),
                () => invalidatingClass.Invalidate( 0, cachedValue0, cachedMethods[2] ),
                () => invalidatingClass.Invalidate( 0, cachedValue0, 0, 5, cachedMethods[3] )
            ];

            this.DoInvalidateCacheAttributeTest(
                _testFromDifferentTypeIgnoringThisParameterProfileName,
                cachedMethods,
                invalidatingMethods,
                "Not matching values test",
                true,
                false );
        }

        #endregion TestFromDifferentTypeIgnoringThisParameter

        #region TestFromDifferentTypeIgnoringThisParameterAsync

        private const string _testFromDifferentTypeIgnoringThisParameterAsyncProfileName =
            _profileNamePrefix + nameof(TestFromDifferentTypeIgnoringThisParameterAsync);

        [CachingConfiguration( ProfileName = _testFromDifferentTypeIgnoringThisParameterAsyncProfileName )]
        private sealed class TestFromDifferentTypeIgnoringThisParameterAsyncCachingClass
        {
            private static int _counter;

            [Cache( IgnoreThisParameter = true )]
            public async Task<CachedValueClass> GetValueAsync()
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache( IgnoreThisParameter = true )]
            public async Task<CachedValueClass> GetValueAsync( int param1 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache( IgnoreThisParameter = true )]
            public async Task<CachedValueClass> GetValueAsync( int param1, CachedValueClass param2 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache( IgnoreThisParameter = true )]
            public async Task<CachedValueClass> GetValueAsync( int param1, CachedValueClass param2, int param3, [NotCacheKey] int param4 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }
        }

        private sealed class TestFromDifferentTypeIgnoringThisParameterAsyncInvalidatingClass
        {
            [InvalidateCache(
                typeof(TestFromDifferentTypeIgnoringThisParameterAsyncCachingClass),
                nameof(TestFromDifferentTypeIgnoringThisParameterAsyncCachingClass.GetValueAsync),
                AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync( Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeIgnoringThisParameterAsyncCachingClass),
                nameof(TestFromDifferentTypeIgnoringThisParameterAsyncCachingClass.GetValueAsync),
                AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync( int param1, Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeIgnoringThisParameterAsyncCachingClass),
                nameof(TestFromDifferentTypeIgnoringThisParameterAsyncCachingClass.GetValueAsync),
                AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync( int param1, CachedValueChildClass param2, Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeIgnoringThisParameterAsyncCachingClass),
                nameof(TestFromDifferentTypeIgnoringThisParameterAsyncCachingClass.GetValueAsync),
                AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync(
                int param1,
                CachedValueChildClass param2,
                int param3,
                int param4,
                Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }
        }

        [Fact]
        public async Task TestFromDifferentTypeIgnoringThisParameterAsync()
        {
            var cachingClass =
                new TestFromDifferentTypeIgnoringThisParameterAsyncCachingClass();

            var invalidatingClass =
                new TestFromDifferentTypeIgnoringThisParameterAsyncInvalidatingClass();

            var cachedValue0 = new CachedValueChildClass( 0 );
            var cachedValue2 = new CachedValueChildClass( 2 );

            var cachedMethods =
                new[]
                {
                    () => cachingClass.GetValueAsync(),
                    () => cachingClass.GetValueAsync( 1 ),
                    () => cachingClass.GetValueAsync( 1, cachedValue2 ),
                    () => cachingClass.GetValueAsync( 1, cachedValue2, 3, 4 )
                };

            var invalidatingMethods =
                new[]
                {
                    () => invalidatingClass.InvalidateAsync( cachedMethods[0] ),
                    () => invalidatingClass.InvalidateAsync( 1, cachedMethods[1] ),
                    () => invalidatingClass.InvalidateAsync( 1, cachedValue2, cachedMethods[2] ),
                    () => invalidatingClass.InvalidateAsync( 1, cachedValue2, 3, 5, cachedMethods[3] )
                };

            await this.DoInvalidateCacheAttributeTestAsync(
                _testFromDifferentTypeIgnoringThisParameterAsyncProfileName,
                cachedMethods,
                invalidatingMethods,
                "Matching values test",
                true,
                true );

            invalidatingMethods =
            [
                () => invalidatingClass.InvalidateAsync( cachedMethods[0] ),
                () => invalidatingClass.InvalidateAsync( 0, cachedMethods[1] ),
                () => invalidatingClass.InvalidateAsync( 0, cachedValue0, cachedMethods[2] ),
                () => invalidatingClass.InvalidateAsync( 0, cachedValue0, 0, 5, cachedMethods[3] )
            ];

            await this.DoInvalidateCacheAttributeTestAsync(
                _testFromDifferentTypeIgnoringThisParameterAsyncProfileName,
                cachedMethods,
                invalidatingMethods,
                "Not matching values test",
                true,
                false );
        }

        #endregion TestFromDifferentTypeIgnoringThisParameterAsync

        #region TestFromDerivedTypeNotIgnoringThisParameter

        private const string _testFromDerivedTypeNotIgnoringThisParameterProfileName = _profileNamePrefix + nameof(TestFromDerivedTypeNotIgnoringThisParameter);

        [CachingConfiguration( ProfileName = _testFromDerivedTypeNotIgnoringThisParameterProfileName )]
        private class TestFromDerivedTypeNotIgnoringThisParameterCachingClass
        {
            private static int _counter;

            [Cache]
            public CachedValueClass GetValue()
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache]
            public CachedValueClass GetValue( int param1 )
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache]
            public CachedValueClass GetValue( int param1, CachedValueClass param2 )
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache]
            public CachedValueClass GetValue( int param1, CachedValueClass param2, int param3, [NotCacheKey] int param4 )
            {
                return new CachedValueClass( _counter++ );
            }
        }

        private sealed class TestFromDerivedTypeNotIgnoringThisParameterInvalidatingClass : TestFromDerivedTypeNotIgnoringThisParameterCachingClass
        {
            [InvalidateCache(
                typeof(TestFromDerivedTypeNotIgnoringThisParameterCachingClass),
                nameof(GetValue),
                AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache(
                typeof(TestFromDerivedTypeNotIgnoringThisParameterCachingClass),
                nameof(GetValue),
                AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( int param1, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache(
                typeof(TestFromDerivedTypeNotIgnoringThisParameterCachingClass),
                nameof(GetValue),
                AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( int param1, CachedValueChildClass param2, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache(
                typeof(TestFromDerivedTypeNotIgnoringThisParameterCachingClass),
                nameof(GetValue),
                AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( int param1, CachedValueChildClass param2, int param3, int param4, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }
        }

        [Fact]
        public void TestFromDerivedTypeNotIgnoringThisParameter()
        {
            var invalidatingAndCachingClass =
                new TestFromDerivedTypeNotIgnoringThisParameterInvalidatingClass();

            var cachedValue0 = new CachedValueChildClass( 0 );
            var cachedValue2 = new CachedValueChildClass( 2 );

            var cachedMethods =
                new[]
                {
                    invalidatingAndCachingClass.GetValue,
                    () => invalidatingAndCachingClass.GetValue( 1 ),
                    () => invalidatingAndCachingClass.GetValue( 1, cachedValue2 ),
                    () => invalidatingAndCachingClass.GetValue( 1, cachedValue2, 3, 4 )
                };

            var invalidatingMethods =
                new[]
                {
                    () => invalidatingAndCachingClass.Invalidate( cachedMethods[0] ),
                    () => invalidatingAndCachingClass.Invalidate( 1, cachedMethods[1] ),
                    () => invalidatingAndCachingClass.Invalidate( 1, cachedValue2, cachedMethods[2] ),
                    () => invalidatingAndCachingClass.Invalidate( 1, cachedValue2, 3, 5, cachedMethods[3] )
                };

            this.DoInvalidateCacheAttributeTest(
                _testFromDerivedTypeNotIgnoringThisParameterProfileName,
                cachedMethods,
                invalidatingMethods,
                "Matching values test",
                true,
                true );

            invalidatingMethods =
            [
                () => invalidatingAndCachingClass.Invalidate( cachedMethods[0] ),
                () => invalidatingAndCachingClass.Invalidate( 0, cachedMethods[1] ),
                () => invalidatingAndCachingClass.Invalidate( 0, cachedValue0, cachedMethods[2] ),
                () => invalidatingAndCachingClass.Invalidate( 0, cachedValue0, 0, 5, cachedMethods[3] )
            ];

            this.DoInvalidateCacheAttributeTest(
                _testFromDerivedTypeNotIgnoringThisParameterProfileName,
                cachedMethods,
                invalidatingMethods,
                "Not matching values test",
                true,
                false );
        }

        #endregion TestFromDerivedTypeNotIgnoringThisParameter

        #region TestFromDerivedTypeNotIgnoringThisParameterAsync

        private const string _testFromDerivedTypeNotIgnoringThisParameterAsyncProfileName =
            _profileNamePrefix + nameof(TestFromDerivedTypeNotIgnoringThisParameterAsync);

        [CachingConfiguration( ProfileName = _testFromDerivedTypeNotIgnoringThisParameterAsyncProfileName )]
        private class TestFromDerivedTypeNotIgnoringThisParameterAsyncCachingClass
        {
            private static int _counter;

            [Cache]
            public async Task<CachedValueClass> GetValueAsync()
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache]
            public async Task<CachedValueClass> GetValueAsync( int param1 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache]
            public async Task<CachedValueClass> GetValueAsync( int param1, CachedValueClass param2 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache]
            public async Task<CachedValueClass> GetValueAsync( int param1, CachedValueClass param2, int param3, [NotCacheKey] int param4 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }
        }

        private sealed class TestFromDerivedTypeNotIgnoringThisParameterAsyncInvalidatingClass : TestFromDerivedTypeNotIgnoringThisParameterAsyncCachingClass
        {
            [InvalidateCache(
                typeof(TestFromDerivedTypeNotIgnoringThisParameterAsyncCachingClass),
                nameof(GetValueAsync),
                AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync( Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache(
                typeof(TestFromDerivedTypeNotIgnoringThisParameterAsyncCachingClass),
                nameof(GetValueAsync),
                AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync( int param1, Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache(
                typeof(TestFromDerivedTypeNotIgnoringThisParameterAsyncCachingClass),
                nameof(GetValueAsync),
                AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync( int param1, CachedValueChildClass param2, Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache(
                typeof(TestFromDerivedTypeNotIgnoringThisParameterAsyncCachingClass),
                nameof(GetValueAsync),
                AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync(
                int param1,
                CachedValueChildClass param2,
                int param3,
                int param4,
                Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }
        }

        [Fact]
        public async Task TestFromDerivedTypeNotIgnoringThisParameterAsync()
        {
            var invalidatingAndCachingClass =
                new TestFromDerivedTypeNotIgnoringThisParameterAsyncInvalidatingClass();

            var cachedValue0 = new CachedValueChildClass( 0 );
            var cachedValue2 = new CachedValueChildClass( 2 );

            var cachedMethods =
                new[]
                {
                    () => invalidatingAndCachingClass.GetValueAsync(),
                    () => invalidatingAndCachingClass.GetValueAsync( 1 ),
                    () => invalidatingAndCachingClass.GetValueAsync( 1, cachedValue2 ),
                    () => invalidatingAndCachingClass.GetValueAsync( 1, cachedValue2, 3, 4 )
                };

            var invalidatingMethods =
                new[]
                {
                    () => invalidatingAndCachingClass.InvalidateAsync( cachedMethods[0] ),
                    () => invalidatingAndCachingClass.InvalidateAsync( 1, cachedMethods[1] ),
                    () => invalidatingAndCachingClass.InvalidateAsync( 1, cachedValue2, cachedMethods[2] ),
                    () => invalidatingAndCachingClass.InvalidateAsync( 1, cachedValue2, 3, 5, cachedMethods[3] )
                };

            await this.DoInvalidateCacheAttributeTestAsync(
                _testFromDerivedTypeNotIgnoringThisParameterAsyncProfileName,
                cachedMethods,
                invalidatingMethods,
                "Matching values test",
                true,
                true );

            invalidatingMethods =
            [
                () => invalidatingAndCachingClass.InvalidateAsync( cachedMethods[0] ),
                () => invalidatingAndCachingClass.InvalidateAsync( 0, cachedMethods[1] ),
                () => invalidatingAndCachingClass.InvalidateAsync( 0, cachedValue0, cachedMethods[2] ),
                () => invalidatingAndCachingClass.InvalidateAsync( 0, cachedValue0, 0, 5, cachedMethods[3] )
            ];

            await this.DoInvalidateCacheAttributeTestAsync(
                _testFromDerivedTypeNotIgnoringThisParameterAsyncProfileName,
                cachedMethods,
                invalidatingMethods,
                "Not matching values test",
                true,
                false );
        }

        #endregion TestFromDerivedTypeNotIgnoringThisParameterAsync

        #region TestFromDifferentTypeInstanceMethodOnStaticMethod

        private const string _testFromDifferentTypeInstanceMethodOnStaticMethodProfileName =
            _profileNamePrefix + nameof(TestFromDifferentTypeInstanceMethodOnStaticMethod);

        [CachingConfiguration( ProfileName = _testFromDifferentTypeInstanceMethodOnStaticMethodProfileName )]
        private sealed class TestFromDifferentTypeInstanceMethodOnStaticMethodCachingClass
        {
            private static int _counter;

            [Cache]
            public static CachedValueClass GetValue()
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache]
            public static CachedValueClass GetValue( int param1 )
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache]
            public static CachedValueClass GetValue( int param1, CachedValueClass param2 )
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache]
            public static CachedValueClass GetValue( int param1, CachedValueClass param2, int param3, [NotCacheKey] int param4 )
            {
                return new CachedValueClass( _counter++ );
            }
        }

        private sealed class TestFromDifferentTypeInstanceMethodOnStaticMethodInvalidatingClass
        {
            [InvalidateCache(
                typeof(TestFromDifferentTypeInstanceMethodOnStaticMethodCachingClass),
                nameof(TestFromDifferentTypeInstanceMethodOnStaticMethodCachingClass.GetValue),
                AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeInstanceMethodOnStaticMethodCachingClass),
                nameof(TestFromDifferentTypeInstanceMethodOnStaticMethodCachingClass.GetValue),
                AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( int param1, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeInstanceMethodOnStaticMethodCachingClass),
                nameof(TestFromDifferentTypeInstanceMethodOnStaticMethodCachingClass.GetValue),
                AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( int param1, CachedValueChildClass param2, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeInstanceMethodOnStaticMethodCachingClass),
                nameof(TestFromDifferentTypeInstanceMethodOnStaticMethodCachingClass.GetValue),
                AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( int param1, CachedValueChildClass param2, int param3, int param4, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }
        }

        [Fact]
        public void TestFromDifferentTypeInstanceMethodOnStaticMethod()
        {
            var invalidatingClass =
                new TestFromDifferentTypeInstanceMethodOnStaticMethodInvalidatingClass();

            var cachedValue0 = new CachedValueChildClass( 0 );
            var cachedValue2 = new CachedValueChildClass( 2 );

            var cachedMethods =
                new[]
                {
                    TestFromDifferentTypeInstanceMethodOnStaticMethodCachingClass.GetValue,
                    () => TestFromDifferentTypeInstanceMethodOnStaticMethodCachingClass.GetValue( 1 ),
                    () => TestFromDifferentTypeInstanceMethodOnStaticMethodCachingClass.GetValue( 1, cachedValue2 ),
                    () => TestFromDifferentTypeInstanceMethodOnStaticMethodCachingClass.GetValue( 1, cachedValue2, 3, 4 )
                };

            var invalidatingMethods =
                new[]
                {
                    () => invalidatingClass.Invalidate( cachedMethods[0] ),
                    () => invalidatingClass.Invalidate( 1, cachedMethods[1] ),
                    () => invalidatingClass.Invalidate( 1, cachedValue2, cachedMethods[2] ),
                    () => invalidatingClass.Invalidate( 1, cachedValue2, 3, 5, cachedMethods[3] )
                };

            this.DoInvalidateCacheAttributeTest(
                _testFromDifferentTypeInstanceMethodOnStaticMethodProfileName,
                cachedMethods,
                invalidatingMethods,
                "Matching values test",
                true,
                true );

            invalidatingMethods =
            [
                () => invalidatingClass.Invalidate( cachedMethods[0] ),
                () => invalidatingClass.Invalidate( 0, cachedMethods[1] ),
                () => invalidatingClass.Invalidate( 0, cachedValue0, cachedMethods[2] ),
                () => invalidatingClass.Invalidate( 0, cachedValue0, 0, 5, cachedMethods[3] )
            ];

            this.DoInvalidateCacheAttributeTest(
                _testFromDifferentTypeInstanceMethodOnStaticMethodProfileName,
                cachedMethods,
                invalidatingMethods,
                "Not matching values test",
                true,
                false );
        }

        #endregion TestFromDifferentTypeInstanceMethodOnStaticMethod

        #region TestFromDifferentTypeInstanceMethodOnStaticMethodAsync

        private const string _testFromDifferentTypeInstanceMethodOnStaticMethodAsyncProfileName =
            _profileNamePrefix + nameof(TestFromDifferentTypeInstanceMethodOnStaticMethodAsync);

        [CachingConfiguration( ProfileName = _testFromDifferentTypeInstanceMethodOnStaticMethodAsyncProfileName )]
        private sealed class TestFromDifferentTypeInstanceMethodOnStaticMethodAsyncCachingClass
        {
            private static int _counter;

            [Cache]
            public static async Task<CachedValueClass> GetValueAsync()
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache]
            public static async Task<CachedValueClass> GetValueAsync( int param1 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache]
            public static async Task<CachedValueClass> GetValueAsync( int param1, CachedValueClass param2 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache]
            public static async Task<CachedValueClass> GetValueAsync( int param1, CachedValueClass param2, int param3, [NotCacheKey] int param4 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }
        }

        private sealed class TestFromDifferentTypeInstanceMethodOnStaticMethodAsyncInvalidatingClass
        {
            [InvalidateCache(
                typeof(TestFromDifferentTypeInstanceMethodOnStaticMethodAsyncCachingClass),
                nameof(TestFromDifferentTypeInstanceMethodOnStaticMethodAsyncCachingClass.GetValueAsync),
                AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync( Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeInstanceMethodOnStaticMethodAsyncCachingClass),
                nameof(TestFromDifferentTypeInstanceMethodOnStaticMethodAsyncCachingClass.GetValueAsync),
                AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync( int param1, Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeInstanceMethodOnStaticMethodAsyncCachingClass),
                nameof(TestFromDifferentTypeInstanceMethodOnStaticMethodAsyncCachingClass.GetValueAsync),
                AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync( int param1, CachedValueChildClass param2, Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeInstanceMethodOnStaticMethodAsyncCachingClass),
                nameof(TestFromDifferentTypeInstanceMethodOnStaticMethodAsyncCachingClass.GetValueAsync),
                AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync(
                int param1,
                CachedValueChildClass param2,
                int param3,
                int param4,
                Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }
        }

        [Fact]
        public async Task TestFromDifferentTypeInstanceMethodOnStaticMethodAsync()
        {
            var invalidatingClass =
                new TestFromDifferentTypeInstanceMethodOnStaticMethodAsyncInvalidatingClass();

            var cachedValue0 = new CachedValueChildClass( 0 );
            var cachedValue2 = new CachedValueChildClass( 2 );

            var cachedMethods =
                new[]
                {
                    () => TestFromDifferentTypeInstanceMethodOnStaticMethodAsyncCachingClass.GetValueAsync(),
                    () => TestFromDifferentTypeInstanceMethodOnStaticMethodAsyncCachingClass.GetValueAsync( 1 ),
                    () => TestFromDifferentTypeInstanceMethodOnStaticMethodAsyncCachingClass.GetValueAsync( 1, cachedValue2 ),
                    () => TestFromDifferentTypeInstanceMethodOnStaticMethodAsyncCachingClass.GetValueAsync( 1, cachedValue2, 3, 4 )
                };

            var invalidatingMethods =
                new[]
                {
                    () => invalidatingClass.InvalidateAsync( cachedMethods[0] ),
                    () => invalidatingClass.InvalidateAsync( 1, cachedMethods[1] ),
                    () => invalidatingClass.InvalidateAsync( 1, cachedValue2, cachedMethods[2] ),
                    () => invalidatingClass.InvalidateAsync( 1, cachedValue2, 3, 5, cachedMethods[3] )
                };

            await this.DoInvalidateCacheAttributeTestAsync(
                _testFromDifferentTypeInstanceMethodOnStaticMethodAsyncProfileName,
                cachedMethods,
                invalidatingMethods,
                "Matching values test",
                true,
                true );

            invalidatingMethods =
            [
                () => invalidatingClass.InvalidateAsync( cachedMethods[0] ),
                () => invalidatingClass.InvalidateAsync( 0, cachedMethods[1] ),
                () => invalidatingClass.InvalidateAsync( 0, cachedValue0, cachedMethods[2] ),
                () => invalidatingClass.InvalidateAsync( 0, cachedValue0, 0, 5, cachedMethods[3] )
            ];

            await this.DoInvalidateCacheAttributeTestAsync(
                _testFromDifferentTypeInstanceMethodOnStaticMethodAsyncProfileName,
                cachedMethods,
                invalidatingMethods,
                "Not matching values test",
                true,
                false );
        }

        #endregion TestFromDifferentTypeInstanceMethodOnStaticMethodAsync

        #region TestFromDifferentTypeStaticMethodOnStaticMethod

        private const string _testFromDifferentTypeStaticMethodOnStaticMethodProfileName =
            _profileNamePrefix + nameof(TestFromDifferentTypeStaticMethodOnStaticMethod);

        [CachingConfiguration( ProfileName = _testFromDifferentTypeStaticMethodOnStaticMethodProfileName )]
        private sealed class TestFromDifferentTypeStaticMethodOnStaticMethodCachingClass
        {
            private static int _counter;

            [Cache]
            public static CachedValueClass GetValue()
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache]
            public static CachedValueClass GetValue( int param1 )
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache]
            public static CachedValueClass GetValue( int param1, CachedValueClass param2 )
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache]
            public static CachedValueClass GetValue( int param1, CachedValueClass param2, int param3, [NotCacheKey] int param4 )
            {
                return new CachedValueClass( _counter++ );
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private sealed class TestFromDifferentTypeStaticMethodOnStaticMethodInvalidatingClass
        {
            [InvalidateCache(
                typeof(TestFromDifferentTypeStaticMethodOnStaticMethodCachingClass),
                nameof(TestFromDifferentTypeStaticMethodOnStaticMethodCachingClass.GetValue),
                AllowMultipleOverloads = true )]
            public static CachedValueClass Invalidate( Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeStaticMethodOnStaticMethodCachingClass),
                nameof(TestFromDifferentTypeStaticMethodOnStaticMethodCachingClass.GetValue),
                AllowMultipleOverloads = true )]
            public static CachedValueClass Invalidate( int param1, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeStaticMethodOnStaticMethodCachingClass),
                nameof(TestFromDifferentTypeStaticMethodOnStaticMethodCachingClass.GetValue),
                AllowMultipleOverloads = true )]
            public static CachedValueClass Invalidate( int param1, CachedValueChildClass param2, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeStaticMethodOnStaticMethodCachingClass),
                nameof(TestFromDifferentTypeStaticMethodOnStaticMethodCachingClass.GetValue),
                AllowMultipleOverloads = true )]
            public static CachedValueClass Invalidate( int param1, CachedValueChildClass param2, int param3, int param4, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }
        }

        [Fact]
        public void TestFromDifferentTypeStaticMethodOnStaticMethod()
        {
            var cachedValue0 = new CachedValueChildClass( 0 );
            var cachedValue2 = new CachedValueChildClass( 2 );

            var cachedMethods =
                new[]
                {
                    TestFromDifferentTypeStaticMethodOnStaticMethodCachingClass.GetValue,
                    () => TestFromDifferentTypeStaticMethodOnStaticMethodCachingClass.GetValue( 1 ),
                    () => TestFromDifferentTypeStaticMethodOnStaticMethodCachingClass.GetValue( 1, cachedValue2 ),
                    () => TestFromDifferentTypeStaticMethodOnStaticMethodCachingClass.GetValue( 1, cachedValue2, 3, 4 )
                };

            var invalidatingMethods =
                new[]
                {
                    () => TestFromDifferentTypeStaticMethodOnStaticMethodInvalidatingClass.Invalidate( cachedMethods[0] ),
                    () => TestFromDifferentTypeStaticMethodOnStaticMethodInvalidatingClass.Invalidate( 1, cachedMethods[1] ),
                    () => TestFromDifferentTypeStaticMethodOnStaticMethodInvalidatingClass.Invalidate( 1, cachedValue2, cachedMethods[2] ),
                    () => TestFromDifferentTypeStaticMethodOnStaticMethodInvalidatingClass.Invalidate( 1, cachedValue2, 3, 5, cachedMethods[3] )
                };

            this.DoInvalidateCacheAttributeTest(
                _testFromDifferentTypeStaticMethodOnStaticMethodProfileName,
                cachedMethods,
                invalidatingMethods,
                "Matching values test",
                true,
                true );

            invalidatingMethods =
            [
                () => TestFromDifferentTypeStaticMethodOnStaticMethodInvalidatingClass.Invalidate( cachedMethods[0] ),
                () => TestFromDifferentTypeStaticMethodOnStaticMethodInvalidatingClass.Invalidate( 0, cachedMethods[1] ),
                () => TestFromDifferentTypeStaticMethodOnStaticMethodInvalidatingClass.Invalidate( 0, cachedValue0, cachedMethods[2] ),
                () => TestFromDifferentTypeStaticMethodOnStaticMethodInvalidatingClass.Invalidate( 0, cachedValue0, 0, 5, cachedMethods[3] )
            ];

            this.DoInvalidateCacheAttributeTest(
                _testFromDifferentTypeStaticMethodOnStaticMethodProfileName,
                cachedMethods,
                invalidatingMethods,
                "Not matching values test",
                true,
                false );
        }

        #endregion TestFromDifferentTypeStaticMethodOnStaticMethod

        #region TestFromDifferentTypeStaticMethodOnStaticMethodAsync

        private const string _testFromDifferentTypeStaticMethodOnStaticMethodAsyncProfileName =
            _profileNamePrefix + nameof(TestFromDifferentTypeStaticMethodOnStaticMethodAsync);

        [CachingConfiguration( ProfileName = _testFromDifferentTypeStaticMethodOnStaticMethodAsyncProfileName )]
        private sealed class TestFromDifferentTypeStaticMethodOnStaticMethodAsyncCachingClass
        {
            private static int _counter;

            [Cache]
            public static async Task<CachedValueClass> GetValueAsync()
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache]
            public static async Task<CachedValueClass> GetValueAsync( int param1 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache]
            public static async Task<CachedValueClass> GetValueAsync( int param1, CachedValueClass param2 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache]
            public static async Task<CachedValueClass> GetValueAsync( int param1, CachedValueClass param2, int param3, [NotCacheKey] int param4 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private sealed class TestFromDifferentTypeStaticMethodOnStaticMethodAsyncInvalidatingClass
        {
            [InvalidateCache(
                typeof(TestFromDifferentTypeStaticMethodOnStaticMethodAsyncCachingClass),
                nameof(TestFromDifferentTypeStaticMethodOnStaticMethodAsyncCachingClass.GetValueAsync),
                AllowMultipleOverloads = true )]
            public static async Task<CachedValueClass> InvalidateAsync( Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeStaticMethodOnStaticMethodAsyncCachingClass),
                nameof(TestFromDifferentTypeStaticMethodOnStaticMethodAsyncCachingClass.GetValueAsync),
                AllowMultipleOverloads = true )]
            public static async Task<CachedValueClass> InvalidateAsync( int param1, Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeStaticMethodOnStaticMethodAsyncCachingClass),
                nameof(TestFromDifferentTypeStaticMethodOnStaticMethodAsyncCachingClass.GetValueAsync),
                AllowMultipleOverloads = true )]
            public static async Task<CachedValueClass> InvalidateAsync( int param1, CachedValueChildClass param2, Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache(
                typeof(TestFromDifferentTypeStaticMethodOnStaticMethodAsyncCachingClass),
                nameof(TestFromDifferentTypeStaticMethodOnStaticMethodAsyncCachingClass.GetValueAsync),
                AllowMultipleOverloads = true )]
            public static async Task<CachedValueClass> InvalidateAsync(
                int param1,
                CachedValueChildClass param2,
                int param3,
                int param4,
                Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }
        }

        [Fact]
        public async Task TestFromDifferentTypeStaticMethodOnStaticMethodAsync()
        {
            var cachedValue0 = new CachedValueChildClass( 0 );
            var cachedValue2 = new CachedValueChildClass( 2 );

            var cachedMethods =
                new[]
                {
                    () => TestFromDifferentTypeStaticMethodOnStaticMethodAsyncCachingClass.GetValueAsync(),
                    () => TestFromDifferentTypeStaticMethodOnStaticMethodAsyncCachingClass.GetValueAsync( 1 ),
                    () => TestFromDifferentTypeStaticMethodOnStaticMethodAsyncCachingClass.GetValueAsync( 1, cachedValue2 ),
                    () => TestFromDifferentTypeStaticMethodOnStaticMethodAsyncCachingClass.GetValueAsync( 1, cachedValue2, 3, 4 )
                };

            var invalidatingMethods =
                new[]
                {
                    () => TestFromDifferentTypeStaticMethodOnStaticMethodAsyncInvalidatingClass.InvalidateAsync( cachedMethods[0] ),
                    () => TestFromDifferentTypeStaticMethodOnStaticMethodAsyncInvalidatingClass.InvalidateAsync( 1, cachedMethods[1] ),
                    () => TestFromDifferentTypeStaticMethodOnStaticMethodAsyncInvalidatingClass
                        .InvalidateAsync( 1, cachedValue2, cachedMethods[2] ),
                    () => TestFromDifferentTypeStaticMethodOnStaticMethodAsyncInvalidatingClass
                        .InvalidateAsync( 1, cachedValue2, 3, 5, cachedMethods[3] )
                };

            await this.DoInvalidateCacheAttributeTestAsync(
                _testFromDifferentTypeStaticMethodOnStaticMethodAsyncProfileName,
                cachedMethods,
                invalidatingMethods,
                "Matching values test",
                true,
                true );

            invalidatingMethods =
            [
                () => TestFromDifferentTypeStaticMethodOnStaticMethodAsyncInvalidatingClass.InvalidateAsync( cachedMethods[0] ),
                () => TestFromDifferentTypeStaticMethodOnStaticMethodAsyncInvalidatingClass.InvalidateAsync( 0, cachedMethods[1] ),
                () => TestFromDifferentTypeStaticMethodOnStaticMethodAsyncInvalidatingClass
                    .InvalidateAsync( 0, cachedValue0, cachedMethods[2] ),
                () => TestFromDifferentTypeStaticMethodOnStaticMethodAsyncInvalidatingClass
                    .InvalidateAsync( 0, cachedValue0, 0, 5, cachedMethods[3] )
            ];

            await this.DoInvalidateCacheAttributeTestAsync(
                _testFromDifferentTypeStaticMethodOnStaticMethodAsyncProfileName,
                cachedMethods,
                invalidatingMethods,
                "Not matching values test",
                true,
                false );
        }

        #endregion TestFromDifferentTypeStaticMethodOnStaticMethodAsync

        #region TestFromTheSameTypeIgnoringThisParameter

        private const string _testFromTheSameTypeIgnoringThisParameterProfileName =
            _profileNamePrefix + nameof(TestFromTheSameTypeIgnoringThisParameter);

        [CachingConfiguration( ProfileName = _testFromTheSameTypeIgnoringThisParameterProfileName )]
        private sealed class TestFromTheSameTypeIgnoringThisParameterInvalidatingAndCachingClass
        {
            private static int _counter;

            [Cache( IgnoreThisParameter = true )]
            public CachedValueClass GetValue()
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache( IgnoreThisParameter = true )]
            public CachedValueClass GetValue( int param1 )
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache( IgnoreThisParameter = true )]
            public CachedValueClass GetValue( int param1, CachedValueClass param2 )
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache( IgnoreThisParameter = true )]
            public CachedValueClass GetValue( int param1, CachedValueClass param2, int param3, [NotCacheKey] int param4 )
            {
                return new CachedValueClass( _counter++ );
            }

            [InvalidateCache( nameof(GetValue), AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache( nameof(GetValue), AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( int param1, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache( nameof(GetValue), AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( int param1, CachedValueChildClass param2, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache( nameof(GetValue), AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( int param1, CachedValueChildClass param2, int param3, int param4, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            // We use ToString() to distinguish target type instances by default
            public override string ToString()
            {
                return $"{this.GetType().Name}@{this.GetHashCode()}";
            }
        }

        [Fact]
        public void TestFromTheSameTypeIgnoringThisParameter()
        {
            var testClass1 =
                new TestFromTheSameTypeIgnoringThisParameterInvalidatingAndCachingClass();

            var testClass2 =
                new TestFromTheSameTypeIgnoringThisParameterInvalidatingAndCachingClass();

            var cachedValue0 = new CachedValueChildClass( 0 );
            var cachedValue2 = new CachedValueChildClass( 2 );

            var cachedMethods =
                new[]
                {
                    testClass1.GetValue,
                    () => testClass1.GetValue( 1 ),
                    () => testClass1.GetValue( 1, cachedValue2 ),
                    () => testClass1.GetValue( 1, cachedValue2, 3, 4 )
                };

            var invalidatingMethods =
                new[]
                {
                    () => testClass1.Invalidate( cachedMethods[0] ),
                    () => testClass1.Invalidate( 1, cachedMethods[1] ),
                    () => testClass1.Invalidate( 1, cachedValue2, cachedMethods[2] ),
                    () => testClass1.Invalidate( 1, cachedValue2, 3, 5, cachedMethods[3] )
                };

            this.DoInvalidateCacheAttributeTest(
                _testFromTheSameTypeIgnoringThisParameterProfileName,
                cachedMethods,
                invalidatingMethods,
                "Matching values test",
                true,
                true );

            invalidatingMethods =
            [
                () => testClass1.Invalidate( cachedMethods[0] ),
                () => testClass1.Invalidate( 0, cachedMethods[1] ),
                () => testClass1.Invalidate( 0, cachedValue0, cachedMethods[2] ),
                () => testClass1.Invalidate( 0, cachedValue0, 0, 5, cachedMethods[3] )
            ];

            this.DoInvalidateCacheAttributeTest(
                _testFromTheSameTypeIgnoringThisParameterProfileName,
                cachedMethods,
                invalidatingMethods,
                "Not matching parameter values with matching this value test",
                true,
                false );

            invalidatingMethods =
            [
                () => testClass2.Invalidate( cachedMethods[0] ),
                () => testClass2.Invalidate( 1, cachedMethods[1] ),
                () => testClass2.Invalidate( 1, cachedValue2, cachedMethods[2] ),
                () => testClass2.Invalidate( 1, cachedValue2, 3, 4, cachedMethods[3] )
            ];

            this.DoInvalidateCacheAttributeTest(
                _testFromTheSameTypeIgnoringThisParameterProfileName,
                cachedMethods,
                invalidatingMethods,
                "Matching parameter values with not matching this value test",
                true,
                true );

            invalidatingMethods =
            [
                () => testClass2.Invalidate( cachedMethods[0] ),
                () => testClass2.Invalidate( 0, cachedMethods[1] ),
                () => testClass2.Invalidate( 0, cachedValue0, cachedMethods[2] ),
                () => testClass2.Invalidate( 0, cachedValue0, 0, 5, cachedMethods[3] )
            ];

            this.DoInvalidateCacheAttributeTest(
                _testFromTheSameTypeIgnoringThisParameterProfileName,
                cachedMethods,
                invalidatingMethods,
                "Not matching parameter values with not matching this value test",
                true,
                false );
        }

        #endregion TestFromTheSameTypeIgnoringThisParameter

        #region TestFromTheSameTypeIgnoringThisParameterAsync

        private const string _testFromTheSameTypeIgnoringThisParameterAsyncProfileName =
            _profileNamePrefix + nameof(TestFromTheSameTypeIgnoringThisParameterAsync);

        [CachingConfiguration( ProfileName = _testFromTheSameTypeIgnoringThisParameterAsyncProfileName )]
        private sealed class TestFromTheSameTypeIgnoringThisParameterAsyncInvalidatingAndCachingClass
        {
            private static int _counter;

            [Cache( IgnoreThisParameter = true )]
            public async Task<CachedValueClass> GetValueAsync()
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache( IgnoreThisParameter = true )]
            public async Task<CachedValueClass> GetValueAsync( int param1 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache( IgnoreThisParameter = true )]
            public async Task<CachedValueClass> GetValueAsync( int param1, CachedValueClass param2 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache( IgnoreThisParameter = true )]
            public async Task<CachedValueClass> GetValueAsync( int param1, CachedValueClass param2, int param3, [NotCacheKey] int param4 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [InvalidateCache( nameof(GetValueAsync), AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync( Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache( nameof(GetValueAsync), AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync( int param1, Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache( nameof(GetValueAsync), AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync( int param1, CachedValueChildClass param2, Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache( nameof(GetValueAsync), AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync(
                int param1,
                CachedValueChildClass param2,
                int param3,
                int param4,
                Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            // We use ToString() to distinguish target type instances by default
            public override string ToString()
            {
                return $"{this.GetType().Name}@{this.GetHashCode()}";
            }
        }

        [Fact]
        public async Task TestFromTheSameTypeIgnoringThisParameterAsync()
        {
            var testClass1 =
                new TestFromTheSameTypeIgnoringThisParameterAsyncInvalidatingAndCachingClass();

            var testClass2 =
                new TestFromTheSameTypeIgnoringThisParameterAsyncInvalidatingAndCachingClass();

            var cachedValue0 = new CachedValueChildClass( 0 );
            var cachedValue2 = new CachedValueChildClass( 2 );

            var cachedMethods =
                new[]
                {
                    () => testClass1.GetValueAsync(),
                    () => testClass1.GetValueAsync( 1 ),
                    () => testClass1.GetValueAsync( 1, cachedValue2 ),
                    () => testClass1.GetValueAsync( 1, cachedValue2, 3, 4 )
                };

            var invalidatingMethods =
                new[]
                {
                    () => testClass1.InvalidateAsync( cachedMethods[0] ),
                    () => testClass1.InvalidateAsync( 1, cachedMethods[1] ),
                    () => testClass1.InvalidateAsync( 1, cachedValue2, cachedMethods[2] ),
                    () => testClass1.InvalidateAsync( 1, cachedValue2, 3, 5, cachedMethods[3] )
                };

            await this.DoInvalidateCacheAttributeTestAsync(
                _testFromTheSameTypeIgnoringThisParameterAsyncProfileName,
                cachedMethods,
                invalidatingMethods,
                "Matching values test",
                true,
                true );

            invalidatingMethods =
            [
                () => testClass1.InvalidateAsync( cachedMethods[0] ),
                () => testClass1.InvalidateAsync( 0, cachedMethods[1] ),
                () => testClass1.InvalidateAsync( 0, cachedValue0, cachedMethods[2] ),
                () => testClass1.InvalidateAsync( 0, cachedValue0, 0, 5, cachedMethods[3] )
            ];

            await this.DoInvalidateCacheAttributeTestAsync(
                _testFromTheSameTypeIgnoringThisParameterAsyncProfileName,
                cachedMethods,
                invalidatingMethods,
                "Not matching parameter values with matching this value test",
                true,
                false );

            invalidatingMethods =
            [
                () => testClass2.InvalidateAsync( cachedMethods[0] ),
                () => testClass2.InvalidateAsync( 1, cachedMethods[1] ),
                () => testClass2.InvalidateAsync( 1, cachedValue2, cachedMethods[2] ),
                () => testClass2.InvalidateAsync( 1, cachedValue2, 3, 4, cachedMethods[3] )
            ];

            await this.DoInvalidateCacheAttributeTestAsync(
                _testFromTheSameTypeIgnoringThisParameterAsyncProfileName,
                cachedMethods,
                invalidatingMethods,
                "Matching parameter values with not matching this value test",
                true,
                true );

            invalidatingMethods =
            [
                () => testClass2.InvalidateAsync( cachedMethods[0] ),
                () => testClass2.InvalidateAsync( 0, cachedMethods[1] ),
                () => testClass2.InvalidateAsync( 0, cachedValue0, cachedMethods[2] ),
                () => testClass2.InvalidateAsync( 0, cachedValue0, 0, 5, cachedMethods[3] )
            ];

            await this.DoInvalidateCacheAttributeTestAsync(
                _testFromTheSameTypeIgnoringThisParameterAsyncProfileName,
                cachedMethods,
                invalidatingMethods,
                "Not matching parameter values with not matching this value test",
                true,
                false );
        }

        #endregion TestFromTheSameTypeIgnoringThisParameterAsync

        #region TestFromTheSameTypeNotIgnoringThisParameter

        private const string _testFromTheSameTypeNotIgnoringThisParameterProfileName =
            _profileNamePrefix + nameof(TestFromTheSameTypeNotIgnoringThisParameter);

        [CachingConfiguration( ProfileName = _testFromTheSameTypeNotIgnoringThisParameterProfileName )]
        private sealed class TestFromTheSameTypeNotIgnoringThisParameterInvalidatingAndCachingClass
        {
            private static int _counter;

            [Cache]
            public CachedValueClass GetValue()
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache]
            public CachedValueClass GetValue( int param1 )
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache]
            public CachedValueClass GetValue( int param1, CachedValueClass param2 )
            {
                return new CachedValueClass( _counter++ );
            }

            [Cache]
            public CachedValueClass GetValue( int param1, CachedValueClass param2, int param3, [NotCacheKey] int param4 )
            {
                return new CachedValueClass( _counter++ );
            }

            [InvalidateCache( nameof(GetValue), AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache( nameof(GetValue), AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( int param1, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache( nameof(GetValue), AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( int param1, CachedValueChildClass param2, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            [InvalidateCache( nameof(GetValue), AllowMultipleOverloads = true )]
            public CachedValueClass Invalidate( int param1, CachedValueChildClass param2, int param3, int param4, Func<CachedValueClass> cachedMethod )
            {
                return cachedMethod.Invoke();
            }

            // We use ToString() to distinguish target type instances by default
            public override string ToString()
            {
                return $"{this.GetType().Name}@{this.GetHashCode()}";
            }
        }

        [Fact]
        public void TestFromTheSameTypeNotIgnoringThisParameter()
        {
            var testClass1 =
                new TestFromTheSameTypeNotIgnoringThisParameterInvalidatingAndCachingClass();

            var testClass2 =
                new TestFromTheSameTypeNotIgnoringThisParameterInvalidatingAndCachingClass();

            var cachedValue0 = new CachedValueChildClass( 0 );
            var cachedValue2 = new CachedValueChildClass( 2 );

            var cachedMethods =
                new[]
                {
                    testClass1.GetValue,
                    () => testClass1.GetValue( 1 ),
                    () => testClass1.GetValue( 1, cachedValue2 ),
                    () => testClass1.GetValue( 1, cachedValue2, 3, 4 )
                };

            var invalidatingMethods =
                new[]
                {
                    () => testClass1.Invalidate( cachedMethods[0] ),
                    () => testClass1.Invalidate( 1, cachedMethods[1] ),
                    () => testClass1.Invalidate( 1, cachedValue2, cachedMethods[2] ),
                    () => testClass1.Invalidate( 1, cachedValue2, 3, 5, cachedMethods[3] )
                };

            this.DoInvalidateCacheAttributeTest(
                _testFromTheSameTypeNotIgnoringThisParameterProfileName,
                cachedMethods,
                invalidatingMethods,
                "Matching values test",
                true,
                true );

            invalidatingMethods =
            [
                () => testClass1.Invalidate( cachedMethods[0] ),
                () => testClass1.Invalidate( 0, cachedMethods[1] ),
                () => testClass1.Invalidate( 0, cachedValue0, cachedMethods[2] ),
                () => testClass1.Invalidate( 0, cachedValue0, 0, 5, cachedMethods[3] )
            ];

            this.DoInvalidateCacheAttributeTest(
                _testFromTheSameTypeNotIgnoringThisParameterProfileName,
                cachedMethods,
                invalidatingMethods,
                "Not matching parameter values with matching this value test",
                true,
                false );

            invalidatingMethods =
            [
                () => testClass2.Invalidate( cachedMethods[0] ),
                () => testClass2.Invalidate( 1, cachedMethods[1] ),
                () => testClass2.Invalidate( 1, cachedValue2, cachedMethods[2] ),
                () => testClass2.Invalidate( 1, cachedValue2, 3, 4, cachedMethods[3] )
            ];

            this.DoInvalidateCacheAttributeTest(
                _testFromTheSameTypeNotIgnoringThisParameterProfileName,
                cachedMethods,
                invalidatingMethods,
                "Matching parameter values with not matching this value test",
                false,
                false );

            invalidatingMethods =
            [
                () => testClass2.Invalidate( cachedMethods[0] ),
                () => testClass2.Invalidate( 0, cachedMethods[1] ),
                () => testClass2.Invalidate( 0, cachedValue0, cachedMethods[2] ),
                () => testClass2.Invalidate( 0, cachedValue0, 0, 5, cachedMethods[3] )
            ];

            this.DoInvalidateCacheAttributeTest(
                _testFromTheSameTypeNotIgnoringThisParameterProfileName,
                cachedMethods,
                invalidatingMethods,
                "Not matching parameter values with not matching this value test",
                false,
                false );
        }

        #endregion TestFromTheSameTypeNotIgnoringThisParameter

        #region TestFromTheSameTypeNotIgnoringThisParameterAsync

        private const string _testFromTheSameTypeNotIgnoringThisParameterAsyncProfileName =
            _profileNamePrefix + nameof(TestFromTheSameTypeNotIgnoringThisParameterAsync);

        [CachingConfiguration( ProfileName = _testFromTheSameTypeNotIgnoringThisParameterAsyncProfileName )]
        private sealed class TestFromTheSameTypeNotIgnoringThisParameterAsyncInvalidatingAndCachingClass
        {
            private static int _counter;

            [Cache]
            public async Task<CachedValueClass> GetValueAsync()
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache]
            public async Task<CachedValueClass> GetValueAsync( int param1 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache]
            public async Task<CachedValueClass> GetValueAsync( int param1, CachedValueClass param2 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [Cache]
            public async Task<CachedValueClass> GetValueAsync( int param1, CachedValueClass param2, int param3, [NotCacheKey] int param4 )
            {
                return await Task.FromResult( new CachedValueClass( _counter++ ) );
            }

            [InvalidateCache( nameof(GetValueAsync), AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync( Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache( nameof(GetValueAsync), AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync( int param1, Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache( nameof(GetValueAsync), AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync( int param1, CachedValueChildClass param2, Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            [InvalidateCache( nameof(GetValueAsync), AllowMultipleOverloads = true )]
            public async Task<CachedValueClass> InvalidateAsync(
                int param1,
                CachedValueChildClass param2,
                int param3,
                int param4,
                Func<Task<CachedValueClass>> cachedMethod )
            {
                return await cachedMethod();
            }

            // We use ToString() to distinguish target type instances by default
            public override string ToString()
            {
                return $"{this.GetType().Name}@{this.GetHashCode()}";
            }
        }

        [Fact]
        public async Task TestFromTheSameTypeNotIgnoringThisParameterAsync()
        {
            var testClass1 =
                new TestFromTheSameTypeNotIgnoringThisParameterAsyncInvalidatingAndCachingClass();

            var testClass2 =
                new TestFromTheSameTypeNotIgnoringThisParameterAsyncInvalidatingAndCachingClass();

            var cachedValue0 = new CachedValueChildClass( 0 );
            var cachedValue2 = new CachedValueChildClass( 2 );

            var cachedMethods =
                new[]
                {
                    () => testClass1.GetValueAsync(),
                    () => testClass1.GetValueAsync( 1 ),
                    () => testClass1.GetValueAsync( 1, cachedValue2 ),
                    () => testClass1.GetValueAsync( 1, cachedValue2, 3, 4 )
                };

            var invalidatingMethods =
                new[]
                {
                    () => testClass1.InvalidateAsync( cachedMethods[0] ),
                    () => testClass1.InvalidateAsync( 1, cachedMethods[1] ),
                    () => testClass1.InvalidateAsync( 1, cachedValue2, cachedMethods[2] ),
                    () => testClass1.InvalidateAsync( 1, cachedValue2, 3, 5, cachedMethods[3] )
                };

            await this.DoInvalidateCacheAttributeTestAsync(
                _testFromTheSameTypeNotIgnoringThisParameterAsyncProfileName,
                cachedMethods,
                invalidatingMethods,
                "Matching values test",
                true,
                true );

            invalidatingMethods =
            [
                () => testClass1.InvalidateAsync( cachedMethods[0] ),
                () => testClass1.InvalidateAsync( 0, cachedMethods[1] ),
                () => testClass1.InvalidateAsync( 0, cachedValue0, cachedMethods[2] ),
                () => testClass1.InvalidateAsync( 0, cachedValue0, 0, 5, cachedMethods[3] )
            ];

            await this.DoInvalidateCacheAttributeTestAsync(
                _testFromTheSameTypeNotIgnoringThisParameterAsyncProfileName,
                cachedMethods,
                invalidatingMethods,
                "Not matching parameter values with matching this value test",
                true,
                false );

            invalidatingMethods =
            [
                () => testClass2.InvalidateAsync( cachedMethods[0] ),
                () => testClass2.InvalidateAsync( 1, cachedMethods[1] ),
                () => testClass2.InvalidateAsync( 1, cachedValue2, cachedMethods[2] ),
                () => testClass2.InvalidateAsync( 1, cachedValue2, 3, 4, cachedMethods[3] )
            ];

            await this.DoInvalidateCacheAttributeTestAsync(
                _testFromTheSameTypeNotIgnoringThisParameterAsyncProfileName,
                cachedMethods,
                invalidatingMethods,
                "Matching parameter values with not matching this value test",
                false,
                false );

            invalidatingMethods =
            [
                () => testClass2.InvalidateAsync( cachedMethods[0] ),
                () => testClass2.InvalidateAsync( 0, cachedMethods[1] ),
                () => testClass2.InvalidateAsync( 0, cachedValue0, cachedMethods[2] ),
                () => testClass2.InvalidateAsync( 0, cachedValue0, 0, 5, cachedMethods[3] )
            ];

            await this.DoInvalidateCacheAttributeTestAsync(
                _testFromTheSameTypeNotIgnoringThisParameterAsyncProfileName,
                cachedMethods,
                invalidatingMethods,
                "Not matching parameter values with not matching this value test",
                false,
                false );
        }

        #endregion TestFromTheSameTypeNotIgnoringThisParameterAsync

        #region TestNestedContexts

        private const string _testNestedContextsProfileName =
            _profileNamePrefix + nameof(TestNestedContexts);

        [CachingConfiguration( ProfileName = _testNestedContextsProfileName )]
        public sealed class TestNestedContextsClass
        {
            private int _invocations;

            [Cache]
            public int OuterMethod()
            {
                return this.InnerMethod();
            }

            [Cache]
            public int InnerMethod()
            {
                return this._invocations++;
            }

            [InvalidateCache( nameof(InnerMethod) )]
            public void InvalidateInnerMethod() { }
        }

        [Fact]
        public void TestNestedContexts()
        {
            using var context = this.InitializeTest( _testNestedContextsProfileName );

            var c = new TestNestedContextsClass();
            var call1 = c.OuterMethod();
            c.InvalidateInnerMethod();
            var call2 = c.OuterMethod();

            Assert.NotEqual( call1, call2 );
        }

        #endregion TestNestedContexts

        #region TestNestedContextsAsync

        private const string _testNestedContextsAsyncProfileName =
            _profileNamePrefix + nameof(TestNestedContextsAsync);

        [CachingConfiguration( ProfileName = _testNestedContextsAsyncProfileName )]
        public sealed class TestNestedContextsAsyncClass
        {
            private int _invocations;

            [Cache]
            public async Task<int> OuterMethodAsync()
            {
                return await this.InnerMethodAsync();
            }

            [Cache]
            public async Task<int> InnerMethodAsync()
            {
                return await Task.FromResult( this._invocations++ );
            }

            [InvalidateCache( nameof(InnerMethodAsync) )]
            public async Task InvalidateInnerMethodAsync()
            {
                await Task.CompletedTask;
            }
        }

        [Fact]
        public async Task TestNestedContextsAsync()
        {
            await using var context = this.InitializeTest( _testNestedContextsAsyncProfileName );

            var c = new TestNestedContextsAsyncClass();
            var call1 = await c.OuterMethodAsync();
            await c.InvalidateInnerMethodAsync();
            var call2 = await c.OuterMethodAsync();

            Assert.NotEqual( call1, call2 );
        }

        #endregion TestNestedContextsAsync

        #region TestParameterMapping

        private const string _testParameterMappingProfileName =
            _profileNamePrefix + nameof(TestParameterMapping);

        [CachingConfiguration( ProfileName = _testParameterMappingProfileName )]
        private sealed class TestParameterMappingInvalidatingAndCachingClass
        {
            private static int _counter;

            [Cache]
            public int GetValue( int id ) { return _counter++; }

            [InvalidateCache( nameof(GetValue) )]
            public void ResetValue( object arg, int id ) { }
        }

        [Fact]
        public void TestParameterMapping()
        {
            using var context = this.InitializeTest( _testParameterMappingProfileName );

            var testClass = new TestParameterMappingInvalidatingAndCachingClass();

            Assert.Equal( 0, testClass.GetValue( 1 ) );
            Assert.Equal( 1, testClass.GetValue( 2 ) );
            Assert.Equal( 1, testClass.GetValue( 2 ) );
            Assert.Equal( 0, testClass.GetValue( 1 ) );

            testClass.ResetValue( new object(), 2 );

            Assert.Equal( 0, testClass.GetValue( 1 ) );
            Assert.Equal( 2, testClass.GetValue( 2 ) );

            testClass.ResetValue( new object(), 1 );

            Assert.Equal( 3, testClass.GetValue( 1 ) );
            Assert.Equal( 2, testClass.GetValue( 2 ) );
        }

        #endregion

        public InvalidateCacheAttributeTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }
    }
}