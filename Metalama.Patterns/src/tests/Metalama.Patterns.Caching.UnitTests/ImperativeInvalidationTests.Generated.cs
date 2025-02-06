// THIS FILE IS T4-GENERATED.
// To edit, go to CacheInvalidation.Generated.tt.
// To transform, run RunT4.ps1.

#pragma warning disable LAMA0320

using System.Threading.Tasks;
using Xunit;
using Metalama.Patterns.Caching.TestHelpers;
using Metalama.Patterns.Caching.Aspects;

namespace Metalama.Patterns.Caching.Tests
{
	public sealed partial class ImperativeInvalidationTests
    {
		
		#region TestSimpleImperativeInvalidationWith1Parameters

        private const string _testSimpleImperativeInvalidationWith1ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith1Parameters);

        class TestSimpleImperativeInvalidationWith1ParametersCachingClass : CachingClass
        {
            [Cache(ProfileName = _testSimpleImperativeInvalidationWith1ParametersProfileName)]
            public override CachedValueClass GetValue( int param1 )
            {
                return base.GetValue( param1 );
            }
        }

        [Fact]
        public void TestSimpleImperativeInvalidationWith1Parameters()
        {
            TestSimpleImperativeInvalidationWith1ParametersCachingClass cachingClass = new TestSimpleImperativeInvalidationWith1ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeInvalidationWith1ParametersProfileName,
                                                () => cachingClass.GetValue( 1 ),
                                                () => CachingService.Default.Invalidate( cachingClass.GetValue, 1 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeInvalidationWith1ParametersAsync

        private const string _testSimpleImperativeInvalidationWith1ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith1ParametersAsync);

        class TestSimpleImperativeInvalidationWith1ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeInvalidationWith1ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1 )
            {
                return await base.GetValueAsync( param1 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeInvalidationWith1ParametersAsync()
        {
            TestSimpleImperativeInvalidationWith1ParametersAsyncCachingClass cachingClass =
                new TestSimpleImperativeInvalidationWith1ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeInvalidationWith1ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1 ),
                                                           () => CachingService.Default.InvalidateAsync( cachingClass.GetValueAsync, 1 ),
                                                           cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith1Parameters

        private const string _testSimpleImperativeRecachingWith1ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith1Parameters);

        class TestSimpleImperativeRecachingWith1ParametersCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith1ParametersProfileName )]
            public override CachedValueClass GetValue( int param1 )
            {
                return base.GetValue( param1 );
            }
        }

        [Fact]
        public void TestSimpleImperativeRecachingWith1Parameters()
        {
            TestSimpleImperativeRecachingWith1ParametersCachingClass cachingClass = new TestSimpleImperativeRecachingWith1ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeRecachingWith1ParametersProfileName,
                                                () => cachingClass.GetValue( 1 ),
                                                () => CachingService.Default.Refresh( cachingClass.GetValue, 1 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith1ParametersAsync

        private const string _testSimpleImperativeRecachingWith1ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith1ParametersAsync);

        class TestSimpleImperativeRecachingWith1ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith1ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1 )
            {
                return await base.GetValueAsync( param1 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeRecachingWith1ParametersAsync()
        {
            TestSimpleImperativeRecachingWith1ParametersAsyncCachingClass cachingClass = new TestSimpleImperativeRecachingWith1ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeRecachingWith1ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1 ),
                                                           () => CachingService.Default.RefreshAsync( cachingClass.GetValueAsync, 1 ),
                                                           cachingClass.Reset );
        }

        #endregion

		
		#region TestSimpleImperativeInvalidationWith2Parameters

        private const string _testSimpleImperativeInvalidationWith2ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith2Parameters);

        class TestSimpleImperativeInvalidationWith2ParametersCachingClass : CachingClass
        {
            [Cache(ProfileName = _testSimpleImperativeInvalidationWith2ParametersProfileName)]
            public override CachedValueClass GetValue( int param1, int param2 )
            {
                return base.GetValue( param1, param2 );
            }
        }

        [Fact]
        public void TestSimpleImperativeInvalidationWith2Parameters()
        {
            TestSimpleImperativeInvalidationWith2ParametersCachingClass cachingClass = new TestSimpleImperativeInvalidationWith2ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeInvalidationWith2ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2 ),
                                                () => CachingService.Default.Invalidate( cachingClass.GetValue, 1, 2 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeInvalidationWith2ParametersAsync

        private const string _testSimpleImperativeInvalidationWith2ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith2ParametersAsync);

        class TestSimpleImperativeInvalidationWith2ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeInvalidationWith2ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2 )
            {
                return await base.GetValueAsync( param1, param2 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeInvalidationWith2ParametersAsync()
        {
            TestSimpleImperativeInvalidationWith2ParametersAsyncCachingClass cachingClass =
                new TestSimpleImperativeInvalidationWith2ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeInvalidationWith2ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2 ),
                                                           () => CachingService.Default.InvalidateAsync( cachingClass.GetValueAsync, 1, 2 ),
                                                           cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith2Parameters

        private const string _testSimpleImperativeRecachingWith2ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith2Parameters);

        class TestSimpleImperativeRecachingWith2ParametersCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith2ParametersProfileName )]
            public override CachedValueClass GetValue( int param1, int param2 )
            {
                return base.GetValue( param1, param2 );
            }
        }

        [Fact]
        public void TestSimpleImperativeRecachingWith2Parameters()
        {
            TestSimpleImperativeRecachingWith2ParametersCachingClass cachingClass = new TestSimpleImperativeRecachingWith2ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeRecachingWith2ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2 ),
                                                () => CachingService.Default.Refresh( cachingClass.GetValue, 1, 2 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith2ParametersAsync

        private const string _testSimpleImperativeRecachingWith2ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith2ParametersAsync);

        class TestSimpleImperativeRecachingWith2ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith2ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2 )
            {
                return await base.GetValueAsync( param1, param2 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeRecachingWith2ParametersAsync()
        {
            TestSimpleImperativeRecachingWith2ParametersAsyncCachingClass cachingClass = new TestSimpleImperativeRecachingWith2ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeRecachingWith2ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2 ),
                                                           () => CachingService.Default.RefreshAsync( cachingClass.GetValueAsync, 1, 2 ),
                                                           cachingClass.Reset );
        }

        #endregion

		
		#region TestSimpleImperativeInvalidationWith3Parameters

        private const string _testSimpleImperativeInvalidationWith3ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith3Parameters);

        class TestSimpleImperativeInvalidationWith3ParametersCachingClass : CachingClass
        {
            [Cache(ProfileName = _testSimpleImperativeInvalidationWith3ParametersProfileName)]
            public override CachedValueClass GetValue( int param1, int param2, int param3 )
            {
                return base.GetValue( param1, param2, param3 );
            }
        }

        [Fact]
        public void TestSimpleImperativeInvalidationWith3Parameters()
        {
            TestSimpleImperativeInvalidationWith3ParametersCachingClass cachingClass = new TestSimpleImperativeInvalidationWith3ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeInvalidationWith3ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2, 3 ),
                                                () => CachingService.Default.Invalidate( cachingClass.GetValue, 1, 2, 3 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeInvalidationWith3ParametersAsync

        private const string _testSimpleImperativeInvalidationWith3ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith3ParametersAsync);

        class TestSimpleImperativeInvalidationWith3ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeInvalidationWith3ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2, int param3 )
            {
                return await base.GetValueAsync( param1, param2, param3 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeInvalidationWith3ParametersAsync()
        {
            TestSimpleImperativeInvalidationWith3ParametersAsyncCachingClass cachingClass =
                new TestSimpleImperativeInvalidationWith3ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeInvalidationWith3ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2, 3 ),
                                                           () => CachingService.Default.InvalidateAsync( cachingClass.GetValueAsync, 1, 2, 3 ),
                                                           cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith3Parameters

        private const string _testSimpleImperativeRecachingWith3ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith3Parameters);

        class TestSimpleImperativeRecachingWith3ParametersCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith3ParametersProfileName )]
            public override CachedValueClass GetValue( int param1, int param2, int param3 )
            {
                return base.GetValue( param1, param2, param3 );
            }
        }

        [Fact]
        public void TestSimpleImperativeRecachingWith3Parameters()
        {
            TestSimpleImperativeRecachingWith3ParametersCachingClass cachingClass = new TestSimpleImperativeRecachingWith3ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeRecachingWith3ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2, 3 ),
                                                () => CachingService.Default.Refresh( cachingClass.GetValue, 1, 2, 3 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith3ParametersAsync

        private const string _testSimpleImperativeRecachingWith3ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith3ParametersAsync);

        class TestSimpleImperativeRecachingWith3ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith3ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2, int param3 )
            {
                return await base.GetValueAsync( param1, param2, param3 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeRecachingWith3ParametersAsync()
        {
            TestSimpleImperativeRecachingWith3ParametersAsyncCachingClass cachingClass = new TestSimpleImperativeRecachingWith3ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeRecachingWith3ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2, 3 ),
                                                           () => CachingService.Default.RefreshAsync( cachingClass.GetValueAsync, 1, 2, 3 ),
                                                           cachingClass.Reset );
        }

        #endregion

		
		#region TestSimpleImperativeInvalidationWith4Parameters

        private const string _testSimpleImperativeInvalidationWith4ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith4Parameters);

        class TestSimpleImperativeInvalidationWith4ParametersCachingClass : CachingClass
        {
            [Cache(ProfileName = _testSimpleImperativeInvalidationWith4ParametersProfileName)]
            public override CachedValueClass GetValue( int param1, int param2, int param3, int param4 )
            {
                return base.GetValue( param1, param2, param3, param4 );
            }
        }

        [Fact]
        public void TestSimpleImperativeInvalidationWith4Parameters()
        {
            TestSimpleImperativeInvalidationWith4ParametersCachingClass cachingClass = new TestSimpleImperativeInvalidationWith4ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeInvalidationWith4ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2, 3, 4 ),
                                                () => CachingService.Default.Invalidate( cachingClass.GetValue, 1, 2, 3, 4 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeInvalidationWith4ParametersAsync

        private const string _testSimpleImperativeInvalidationWith4ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith4ParametersAsync);

        class TestSimpleImperativeInvalidationWith4ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeInvalidationWith4ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2, int param3, int param4 )
            {
                return await base.GetValueAsync( param1, param2, param3, param4 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeInvalidationWith4ParametersAsync()
        {
            TestSimpleImperativeInvalidationWith4ParametersAsyncCachingClass cachingClass =
                new TestSimpleImperativeInvalidationWith4ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeInvalidationWith4ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2, 3, 4 ),
                                                           () => CachingService.Default.InvalidateAsync( cachingClass.GetValueAsync, 1, 2, 3, 4 ),
                                                           cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith4Parameters

        private const string _testSimpleImperativeRecachingWith4ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith4Parameters);

        class TestSimpleImperativeRecachingWith4ParametersCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith4ParametersProfileName )]
            public override CachedValueClass GetValue( int param1, int param2, int param3, int param4 )
            {
                return base.GetValue( param1, param2, param3, param4 );
            }
        }

        [Fact]
        public void TestSimpleImperativeRecachingWith4Parameters()
        {
            TestSimpleImperativeRecachingWith4ParametersCachingClass cachingClass = new TestSimpleImperativeRecachingWith4ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeRecachingWith4ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2, 3, 4 ),
                                                () => CachingService.Default.Refresh( cachingClass.GetValue, 1, 2, 3, 4 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith4ParametersAsync

        private const string _testSimpleImperativeRecachingWith4ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith4ParametersAsync);

        class TestSimpleImperativeRecachingWith4ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith4ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2, int param3, int param4 )
            {
                return await base.GetValueAsync( param1, param2, param3, param4 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeRecachingWith4ParametersAsync()
        {
            TestSimpleImperativeRecachingWith4ParametersAsyncCachingClass cachingClass = new TestSimpleImperativeRecachingWith4ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeRecachingWith4ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2, 3, 4 ),
                                                           () => CachingService.Default.RefreshAsync( cachingClass.GetValueAsync, 1, 2, 3, 4 ),
                                                           cachingClass.Reset );
        }

        #endregion

		
		#region TestSimpleImperativeInvalidationWith5Parameters

        private const string _testSimpleImperativeInvalidationWith5ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith5Parameters);

        class TestSimpleImperativeInvalidationWith5ParametersCachingClass : CachingClass
        {
            [Cache(ProfileName = _testSimpleImperativeInvalidationWith5ParametersProfileName)]
            public override CachedValueClass GetValue( int param1, int param2, int param3, int param4, int param5 )
            {
                return base.GetValue( param1, param2, param3, param4, param5 );
            }
        }

        [Fact]
        public void TestSimpleImperativeInvalidationWith5Parameters()
        {
            TestSimpleImperativeInvalidationWith5ParametersCachingClass cachingClass = new TestSimpleImperativeInvalidationWith5ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeInvalidationWith5ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2, 3, 4, 5 ),
                                                () => CachingService.Default.Invalidate( cachingClass.GetValue, 1, 2, 3, 4, 5 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeInvalidationWith5ParametersAsync

        private const string _testSimpleImperativeInvalidationWith5ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith5ParametersAsync);

        class TestSimpleImperativeInvalidationWith5ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeInvalidationWith5ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2, int param3, int param4, int param5 )
            {
                return await base.GetValueAsync( param1, param2, param3, param4, param5 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeInvalidationWith5ParametersAsync()
        {
            TestSimpleImperativeInvalidationWith5ParametersAsyncCachingClass cachingClass =
                new TestSimpleImperativeInvalidationWith5ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeInvalidationWith5ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2, 3, 4, 5 ),
                                                           () => CachingService.Default.InvalidateAsync( cachingClass.GetValueAsync, 1, 2, 3, 4, 5 ),
                                                           cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith5Parameters

        private const string _testSimpleImperativeRecachingWith5ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith5Parameters);

        class TestSimpleImperativeRecachingWith5ParametersCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith5ParametersProfileName )]
            public override CachedValueClass GetValue( int param1, int param2, int param3, int param4, int param5 )
            {
                return base.GetValue( param1, param2, param3, param4, param5 );
            }
        }

        [Fact]
        public void TestSimpleImperativeRecachingWith5Parameters()
        {
            TestSimpleImperativeRecachingWith5ParametersCachingClass cachingClass = new TestSimpleImperativeRecachingWith5ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeRecachingWith5ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2, 3, 4, 5 ),
                                                () => CachingService.Default.Refresh( cachingClass.GetValue, 1, 2, 3, 4, 5 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith5ParametersAsync

        private const string _testSimpleImperativeRecachingWith5ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith5ParametersAsync);

        class TestSimpleImperativeRecachingWith5ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith5ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2, int param3, int param4, int param5 )
            {
                return await base.GetValueAsync( param1, param2, param3, param4, param5 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeRecachingWith5ParametersAsync()
        {
            TestSimpleImperativeRecachingWith5ParametersAsyncCachingClass cachingClass = new TestSimpleImperativeRecachingWith5ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeRecachingWith5ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2, 3, 4, 5 ),
                                                           () => CachingService.Default.RefreshAsync( cachingClass.GetValueAsync, 1, 2, 3, 4, 5 ),
                                                           cachingClass.Reset );
        }

        #endregion

		
		#region TestSimpleImperativeInvalidationWith6Parameters

        private const string _testSimpleImperativeInvalidationWith6ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith6Parameters);

        class TestSimpleImperativeInvalidationWith6ParametersCachingClass : CachingClass
        {
            [Cache(ProfileName = _testSimpleImperativeInvalidationWith6ParametersProfileName)]
            public override CachedValueClass GetValue( int param1, int param2, int param3, int param4, int param5, int param6 )
            {
                return base.GetValue( param1, param2, param3, param4, param5, param6 );
            }
        }

        [Fact]
        public void TestSimpleImperativeInvalidationWith6Parameters()
        {
            TestSimpleImperativeInvalidationWith6ParametersCachingClass cachingClass = new TestSimpleImperativeInvalidationWith6ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeInvalidationWith6ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2, 3, 4, 5, 6 ),
                                                () => CachingService.Default.Invalidate( cachingClass.GetValue, 1, 2, 3, 4, 5, 6 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeInvalidationWith6ParametersAsync

        private const string _testSimpleImperativeInvalidationWith6ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith6ParametersAsync);

        class TestSimpleImperativeInvalidationWith6ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeInvalidationWith6ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2, int param3, int param4, int param5, int param6 )
            {
                return await base.GetValueAsync( param1, param2, param3, param4, param5, param6 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeInvalidationWith6ParametersAsync()
        {
            TestSimpleImperativeInvalidationWith6ParametersAsyncCachingClass cachingClass =
                new TestSimpleImperativeInvalidationWith6ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeInvalidationWith6ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2, 3, 4, 5, 6 ),
                                                           () => CachingService.Default.InvalidateAsync( cachingClass.GetValueAsync, 1, 2, 3, 4, 5, 6 ),
                                                           cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith6Parameters

        private const string _testSimpleImperativeRecachingWith6ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith6Parameters);

        class TestSimpleImperativeRecachingWith6ParametersCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith6ParametersProfileName )]
            public override CachedValueClass GetValue( int param1, int param2, int param3, int param4, int param5, int param6 )
            {
                return base.GetValue( param1, param2, param3, param4, param5, param6 );
            }
        }

        [Fact]
        public void TestSimpleImperativeRecachingWith6Parameters()
        {
            TestSimpleImperativeRecachingWith6ParametersCachingClass cachingClass = new TestSimpleImperativeRecachingWith6ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeRecachingWith6ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2, 3, 4, 5, 6 ),
                                                () => CachingService.Default.Refresh( cachingClass.GetValue, 1, 2, 3, 4, 5, 6 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith6ParametersAsync

        private const string _testSimpleImperativeRecachingWith6ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith6ParametersAsync);

        class TestSimpleImperativeRecachingWith6ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith6ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2, int param3, int param4, int param5, int param6 )
            {
                return await base.GetValueAsync( param1, param2, param3, param4, param5, param6 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeRecachingWith6ParametersAsync()
        {
            TestSimpleImperativeRecachingWith6ParametersAsyncCachingClass cachingClass = new TestSimpleImperativeRecachingWith6ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeRecachingWith6ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2, 3, 4, 5, 6 ),
                                                           () => CachingService.Default.RefreshAsync( cachingClass.GetValueAsync, 1, 2, 3, 4, 5, 6 ),
                                                           cachingClass.Reset );
        }

        #endregion

		
		#region TestSimpleImperativeInvalidationWith7Parameters

        private const string _testSimpleImperativeInvalidationWith7ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith7Parameters);

        class TestSimpleImperativeInvalidationWith7ParametersCachingClass : CachingClass
        {
            [Cache(ProfileName = _testSimpleImperativeInvalidationWith7ParametersProfileName)]
            public override CachedValueClass GetValue( int param1, int param2, int param3, int param4, int param5, int param6, int param7 )
            {
                return base.GetValue( param1, param2, param3, param4, param5, param6, param7 );
            }
        }

        [Fact]
        public void TestSimpleImperativeInvalidationWith7Parameters()
        {
            TestSimpleImperativeInvalidationWith7ParametersCachingClass cachingClass = new TestSimpleImperativeInvalidationWith7ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeInvalidationWith7ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2, 3, 4, 5, 6, 7 ),
                                                () => CachingService.Default.Invalidate( cachingClass.GetValue, 1, 2, 3, 4, 5, 6, 7 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeInvalidationWith7ParametersAsync

        private const string _testSimpleImperativeInvalidationWith7ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith7ParametersAsync);

        class TestSimpleImperativeInvalidationWith7ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeInvalidationWith7ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2, int param3, int param4, int param5, int param6, int param7 )
            {
                return await base.GetValueAsync( param1, param2, param3, param4, param5, param6, param7 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeInvalidationWith7ParametersAsync()
        {
            TestSimpleImperativeInvalidationWith7ParametersAsyncCachingClass cachingClass =
                new TestSimpleImperativeInvalidationWith7ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeInvalidationWith7ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2, 3, 4, 5, 6, 7 ),
                                                           () => CachingService.Default.InvalidateAsync( cachingClass.GetValueAsync, 1, 2, 3, 4, 5, 6, 7 ),
                                                           cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith7Parameters

        private const string _testSimpleImperativeRecachingWith7ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith7Parameters);

        class TestSimpleImperativeRecachingWith7ParametersCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith7ParametersProfileName )]
            public override CachedValueClass GetValue( int param1, int param2, int param3, int param4, int param5, int param6, int param7 )
            {
                return base.GetValue( param1, param2, param3, param4, param5, param6, param7 );
            }
        }

        [Fact]
        public void TestSimpleImperativeRecachingWith7Parameters()
        {
            TestSimpleImperativeRecachingWith7ParametersCachingClass cachingClass = new TestSimpleImperativeRecachingWith7ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeRecachingWith7ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2, 3, 4, 5, 6, 7 ),
                                                () => CachingService.Default.Refresh( cachingClass.GetValue, 1, 2, 3, 4, 5, 6, 7 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith7ParametersAsync

        private const string _testSimpleImperativeRecachingWith7ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith7ParametersAsync);

        class TestSimpleImperativeRecachingWith7ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith7ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2, int param3, int param4, int param5, int param6, int param7 )
            {
                return await base.GetValueAsync( param1, param2, param3, param4, param5, param6, param7 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeRecachingWith7ParametersAsync()
        {
            TestSimpleImperativeRecachingWith7ParametersAsyncCachingClass cachingClass = new TestSimpleImperativeRecachingWith7ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeRecachingWith7ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2, 3, 4, 5, 6, 7 ),
                                                           () => CachingService.Default.RefreshAsync( cachingClass.GetValueAsync, 1, 2, 3, 4, 5, 6, 7 ),
                                                           cachingClass.Reset );
        }

        #endregion

		
		#region TestSimpleImperativeInvalidationWith8Parameters

        private const string _testSimpleImperativeInvalidationWith8ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith8Parameters);

        class TestSimpleImperativeInvalidationWith8ParametersCachingClass : CachingClass
        {
            [Cache(ProfileName = _testSimpleImperativeInvalidationWith8ParametersProfileName)]
            public override CachedValueClass GetValue( int param1, int param2, int param3, int param4, int param5, int param6, int param7, int param8 )
            {
                return base.GetValue( param1, param2, param3, param4, param5, param6, param7, param8 );
            }
        }

        [Fact]
        public void TestSimpleImperativeInvalidationWith8Parameters()
        {
            TestSimpleImperativeInvalidationWith8ParametersCachingClass cachingClass = new TestSimpleImperativeInvalidationWith8ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeInvalidationWith8ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2, 3, 4, 5, 6, 7, 8 ),
                                                () => CachingService.Default.Invalidate( cachingClass.GetValue, 1, 2, 3, 4, 5, 6, 7, 8 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeInvalidationWith8ParametersAsync

        private const string _testSimpleImperativeInvalidationWith8ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith8ParametersAsync);

        class TestSimpleImperativeInvalidationWith8ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeInvalidationWith8ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2, int param3, int param4, int param5, int param6, int param7, int param8 )
            {
                return await base.GetValueAsync( param1, param2, param3, param4, param5, param6, param7, param8 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeInvalidationWith8ParametersAsync()
        {
            TestSimpleImperativeInvalidationWith8ParametersAsyncCachingClass cachingClass =
                new TestSimpleImperativeInvalidationWith8ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeInvalidationWith8ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2, 3, 4, 5, 6, 7, 8 ),
                                                           () => CachingService.Default.InvalidateAsync( cachingClass.GetValueAsync, 1, 2, 3, 4, 5, 6, 7, 8 ),
                                                           cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith8Parameters

        private const string _testSimpleImperativeRecachingWith8ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith8Parameters);

        class TestSimpleImperativeRecachingWith8ParametersCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith8ParametersProfileName )]
            public override CachedValueClass GetValue( int param1, int param2, int param3, int param4, int param5, int param6, int param7, int param8 )
            {
                return base.GetValue( param1, param2, param3, param4, param5, param6, param7, param8 );
            }
        }

        [Fact]
        public void TestSimpleImperativeRecachingWith8Parameters()
        {
            TestSimpleImperativeRecachingWith8ParametersCachingClass cachingClass = new TestSimpleImperativeRecachingWith8ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeRecachingWith8ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2, 3, 4, 5, 6, 7, 8 ),
                                                () => CachingService.Default.Refresh( cachingClass.GetValue, 1, 2, 3, 4, 5, 6, 7, 8 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith8ParametersAsync

        private const string _testSimpleImperativeRecachingWith8ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith8ParametersAsync);

        class TestSimpleImperativeRecachingWith8ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith8ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2, int param3, int param4, int param5, int param6, int param7, int param8 )
            {
                return await base.GetValueAsync( param1, param2, param3, param4, param5, param6, param7, param8 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeRecachingWith8ParametersAsync()
        {
            TestSimpleImperativeRecachingWith8ParametersAsyncCachingClass cachingClass = new TestSimpleImperativeRecachingWith8ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeRecachingWith8ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2, 3, 4, 5, 6, 7, 8 ),
                                                           () => CachingService.Default.RefreshAsync( cachingClass.GetValueAsync, 1, 2, 3, 4, 5, 6, 7, 8 ),
                                                           cachingClass.Reset );
        }

        #endregion

		
		#region TestSimpleImperativeInvalidationWith9Parameters

        private const string _testSimpleImperativeInvalidationWith9ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith9Parameters);

        class TestSimpleImperativeInvalidationWith9ParametersCachingClass : CachingClass
        {
            [Cache(ProfileName = _testSimpleImperativeInvalidationWith9ParametersProfileName)]
            public override CachedValueClass GetValue( int param1, int param2, int param3, int param4, int param5, int param6, int param7, int param8, int param9 )
            {
                return base.GetValue( param1, param2, param3, param4, param5, param6, param7, param8, param9 );
            }
        }

        [Fact]
        public void TestSimpleImperativeInvalidationWith9Parameters()
        {
            TestSimpleImperativeInvalidationWith9ParametersCachingClass cachingClass = new TestSimpleImperativeInvalidationWith9ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeInvalidationWith9ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2, 3, 4, 5, 6, 7, 8, 9 ),
                                                () => CachingService.Default.Invalidate( cachingClass.GetValue, 1, 2, 3, 4, 5, 6, 7, 8, 9 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeInvalidationWith9ParametersAsync

        private const string _testSimpleImperativeInvalidationWith9ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith9ParametersAsync);

        class TestSimpleImperativeInvalidationWith9ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeInvalidationWith9ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2, int param3, int param4, int param5, int param6, int param7, int param8, int param9 )
            {
                return await base.GetValueAsync( param1, param2, param3, param4, param5, param6, param7, param8, param9 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeInvalidationWith9ParametersAsync()
        {
            TestSimpleImperativeInvalidationWith9ParametersAsyncCachingClass cachingClass =
                new TestSimpleImperativeInvalidationWith9ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeInvalidationWith9ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2, 3, 4, 5, 6, 7, 8, 9 ),
                                                           () => CachingService.Default.InvalidateAsync( cachingClass.GetValueAsync, 1, 2, 3, 4, 5, 6, 7, 8, 9 ),
                                                           cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith9Parameters

        private const string _testSimpleImperativeRecachingWith9ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith9Parameters);

        class TestSimpleImperativeRecachingWith9ParametersCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith9ParametersProfileName )]
            public override CachedValueClass GetValue( int param1, int param2, int param3, int param4, int param5, int param6, int param7, int param8, int param9 )
            {
                return base.GetValue( param1, param2, param3, param4, param5, param6, param7, param8, param9 );
            }
        }

        [Fact]
        public void TestSimpleImperativeRecachingWith9Parameters()
        {
            TestSimpleImperativeRecachingWith9ParametersCachingClass cachingClass = new TestSimpleImperativeRecachingWith9ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeRecachingWith9ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2, 3, 4, 5, 6, 7, 8, 9 ),
                                                () => CachingService.Default.Refresh( cachingClass.GetValue, 1, 2, 3, 4, 5, 6, 7, 8, 9 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith9ParametersAsync

        private const string _testSimpleImperativeRecachingWith9ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith9ParametersAsync);

        class TestSimpleImperativeRecachingWith9ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith9ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2, int param3, int param4, int param5, int param6, int param7, int param8, int param9 )
            {
                return await base.GetValueAsync( param1, param2, param3, param4, param5, param6, param7, param8, param9 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeRecachingWith9ParametersAsync()
        {
            TestSimpleImperativeRecachingWith9ParametersAsyncCachingClass cachingClass = new TestSimpleImperativeRecachingWith9ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeRecachingWith9ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2, 3, 4, 5, 6, 7, 8, 9 ),
                                                           () => CachingService.Default.RefreshAsync( cachingClass.GetValueAsync, 1, 2, 3, 4, 5, 6, 7, 8, 9 ),
                                                           cachingClass.Reset );
        }

        #endregion

		
		#region TestSimpleImperativeInvalidationWith10Parameters

        private const string _testSimpleImperativeInvalidationWith10ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith10Parameters);

        class TestSimpleImperativeInvalidationWith10ParametersCachingClass : CachingClass
        {
            [Cache(ProfileName = _testSimpleImperativeInvalidationWith10ParametersProfileName)]
            public override CachedValueClass GetValue( int param1, int param2, int param3, int param4, int param5, int param6, int param7, int param8, int param9, int param10 )
            {
                return base.GetValue( param1, param2, param3, param4, param5, param6, param7, param8, param9, param10 );
            }
        }

        [Fact]
        public void TestSimpleImperativeInvalidationWith10Parameters()
        {
            TestSimpleImperativeInvalidationWith10ParametersCachingClass cachingClass = new TestSimpleImperativeInvalidationWith10ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeInvalidationWith10ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 ),
                                                () => CachingService.Default.Invalidate( cachingClass.GetValue, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeInvalidationWith10ParametersAsync

        private const string _testSimpleImperativeInvalidationWith10ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeInvalidationWith10ParametersAsync);

        class TestSimpleImperativeInvalidationWith10ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeInvalidationWith10ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2, int param3, int param4, int param5, int param6, int param7, int param8, int param9, int param10 )
            {
                return await base.GetValueAsync( param1, param2, param3, param4, param5, param6, param7, param8, param9, param10 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeInvalidationWith10ParametersAsync()
        {
            TestSimpleImperativeInvalidationWith10ParametersAsyncCachingClass cachingClass =
                new TestSimpleImperativeInvalidationWith10ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeInvalidationWith10ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 ),
                                                           () => CachingService.Default.InvalidateAsync( cachingClass.GetValueAsync, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 ),
                                                           cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith10Parameters

        private const string _testSimpleImperativeRecachingWith10ParametersProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith10Parameters);

        class TestSimpleImperativeRecachingWith10ParametersCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith10ParametersProfileName )]
            public override CachedValueClass GetValue( int param1, int param2, int param3, int param4, int param5, int param6, int param7, int param8, int param9, int param10 )
            {
                return base.GetValue( param1, param2, param3, param4, param5, param6, param7, param8, param9, param10 );
            }
        }

        [Fact]
        public void TestSimpleImperativeRecachingWith10Parameters()
        {
            TestSimpleImperativeRecachingWith10ParametersCachingClass cachingClass = new TestSimpleImperativeRecachingWith10ParametersCachingClass();

            DoTestSimpleImperativeInvalidation( _testSimpleImperativeRecachingWith10ParametersProfileName,
                                                () => cachingClass.GetValue( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 ),
                                                () => CachingService.Default.Refresh( cachingClass.GetValue, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 ),
                                                cachingClass.Reset );
        }

        #endregion

		#region TestSimpleImperativeRecachingWith10ParametersAsync

        private const string _testSimpleImperativeRecachingWith10ParametersAsyncProfileName =
            _profileNamePrefix + nameof(TestSimpleImperativeRecachingWith10ParametersAsync);

        class TestSimpleImperativeRecachingWith10ParametersAsyncCachingClass : CachingClass
        {
            [Cache( ProfileName = _testSimpleImperativeRecachingWith10ParametersAsyncProfileName )]
            public override async Task<CachedValueClass> GetValueAsync( int param1, int param2, int param3, int param4, int param5, int param6, int param7, int param8, int param9, int param10 )
            {
                return await base.GetValueAsync( param1, param2, param3, param4, param5, param6, param7, param8, param9, param10 );
            }
        }

        [Fact]
        public async Task TestSimpleImperativeRecachingWith10ParametersAsync()
        {
            TestSimpleImperativeRecachingWith10ParametersAsyncCachingClass cachingClass = new TestSimpleImperativeRecachingWith10ParametersAsyncCachingClass();

            await DoTestSimpleImperativeInvalidationAsync( _testSimpleImperativeRecachingWith10ParametersAsyncProfileName,
                                                           () => cachingClass.GetValueAsync( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 ),
                                                           () => CachingService.Default.RefreshAsync( cachingClass.GetValueAsync, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 ),
                                                           cachingClass.Reset );
        }

        #endregion

		
	}
}

