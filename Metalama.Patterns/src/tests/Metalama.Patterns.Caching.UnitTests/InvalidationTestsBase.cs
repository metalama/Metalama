// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Caching.Tests
{
    public abstract class InvalidationTestsBase : BaseCachingTests
    {
        protected sealed class CallsCounters
        {
            private readonly int[] _counters;

            public int this[ int i ] => this._counters[i];

            public CallsCounters( int size )
            {
                this._counters = new int[size];

                for ( var i = 0; i < this._counters.Length; i++ )
                {
                    this._counters[i] = 0;
                }
            }

            public void Increment( int i )
            {
                this._counters[i]++;
            }
        }

        protected void DoInvalidateCacheAttributeTest(
            string profileName,
            Func<CachedValueClass>[] cachedMethods,
            Func<CachedValueClass>[] invalidatingMethods,
            string testDescription,
            bool firstShouldWork,
            bool othersShouldWork,
            bool onlyPairsShouldWork = false )
        {
            using var context = this.InitializeTest( profileName );

            Assert.Equal( cachedMethods.Length, invalidatingMethods.Length );

            var cachedMethodValuesBeforeInvalidation = new CachedValueClass[cachedMethods.Length];

            for ( var testedPairIndex = 0; testedPairIndex < cachedMethods.Length; testedPairIndex++ )
            {
                for ( var cachedMethodIndex = 0; cachedMethodIndex < cachedMethods.Length; cachedMethodIndex++ )
                {
                    cachedMethodValuesBeforeInvalidation[cachedMethodIndex] =
                        cachedMethods[cachedMethodIndex].Invoke();
                }

                var valueDuringInvalidation =
                    invalidatingMethods[testedPairIndex].Invoke();

                AssertEx.Equal(
                    cachedMethodValuesBeforeInvalidation[testedPairIndex],
                    valueDuringInvalidation,
                    $"{testDescription}: The value during invalidation of the invalidated method #{testedPairIndex} is different than the cached one when invalidating by the method #{testedPairIndex}." );

                for ( var cachedMethodIndex = 0; cachedMethodIndex < cachedMethods.Length; cachedMethodIndex++ )
                {
                    var cachedMethodValueAfterInvalidation =
                        cachedMethods[cachedMethodIndex].Invoke();

                    bool shouldBeInvalidated;

                    if ( onlyPairsShouldWork )
                    {
                        shouldBeInvalidated = cachedMethodIndex == testedPairIndex
                                              && ((firstShouldWork && testedPairIndex == 0)
                                                  || (othersShouldWork && testedPairIndex > 0));
                    }
                    else
                    {
                        shouldBeInvalidated = cachedMethodIndex <= testedPairIndex
                                              && ((firstShouldWork && cachedMethodIndex == 0)
                                                  || (othersShouldWork && cachedMethodIndex > 0));
                    }

                    if ( shouldBeInvalidated )
                    {
                        AssertEx.NotEqual(
                            cachedMethodValuesBeforeInvalidation[cachedMethodIndex],
                            cachedMethodValueAfterInvalidation,
                            $"{testDescription}: The value after invalidation of the invalidated method #{cachedMethodIndex} is the same as the cached one when invalidating by the method #{testedPairIndex}." );
                    }
                    else
                    {
                        AssertEx.Equal(
                            cachedMethodValuesBeforeInvalidation[cachedMethodIndex],
                            cachedMethodValueAfterInvalidation,
                            $"{testDescription}: The value after invalidation of the not invalidated method #{cachedMethodIndex} is different than the cached one when invalidating by the method #{testedPairIndex}." );
                    }
                }
            }
        }

        protected async Task DoInvalidateCacheAttributeTestAsync(
            string profileName,
            IReadOnlyList<Func<Task<CachedValueClass>>> cachedMethods,
            IReadOnlyList<Func<Task>> invalidatingMethods,
            string testDescription,
            bool firstShouldWork,
            bool othersShouldWork,
            bool onlyPairsShouldWork = false,
            bool invalidateMustReturnNewValue = false )
        {
            await using var context = this.InitializeTest( profileName );

            Assert.Equal( cachedMethods.Count, invalidatingMethods.Count );

            var cachedMethodValuesBeforeInvalidation = new CachedValueClass[cachedMethods.Count];

            for ( var testedPairIndex = 0; testedPairIndex < cachedMethods.Count; testedPairIndex++ )
            {
                // Execute all cached methods.
                for ( var cachedMethodIndex = 0; cachedMethodIndex < cachedMethods.Count; cachedMethodIndex++ )
                {
                    cachedMethodValuesBeforeInvalidation[cachedMethodIndex] = await cachedMethods[cachedMethodIndex].Invoke();
                }

                // Execute the current invalidating method.
                if ( invalidatingMethods[testedPairIndex] is Func<Task<CachedValueClass>> nonVoidMethod )
                {
                    var valueDuringInvalidation = await nonVoidMethod.Invoke();

                    if ( invalidateMustReturnNewValue )
                    {
                        AssertEx.NotEqual(
                            cachedMethodValuesBeforeInvalidation[testedPairIndex],
                            valueDuringInvalidation,
                            $"{testDescription}: The value during invalidation of the invalidated method #{testedPairIndex} is the same as the cached one when invalidating by the method #{testedPairIndex}." );
                    }
                    else
                    {
                        AssertEx.Equal(
                            cachedMethodValuesBeforeInvalidation[testedPairIndex],
                            valueDuringInvalidation,
                            $"{testDescription}: The value during invalidation of the invalidated method #{testedPairIndex} is different than the cached one when invalidating by the method #{testedPairIndex}." );
                    }
                }
                else
                {
                    await invalidatingMethods[testedPairIndex].Invoke();
                }

                // Execute all cached methods a second time.
                for ( var cachedMethodIndex = 0; cachedMethodIndex < cachedMethods.Count; cachedMethodIndex++ )
                {
                    var cachedMethodValueAfterInvalidation = await cachedMethods[cachedMethodIndex].Invoke();

                    bool shouldBeInvalidated;

                    if ( onlyPairsShouldWork )
                    {
                        shouldBeInvalidated = cachedMethodIndex == testedPairIndex
                                              && ((firstShouldWork && testedPairIndex == 0)
                                                  || (othersShouldWork && testedPairIndex > 0));
                    }
                    else
                    {
                        shouldBeInvalidated = cachedMethodIndex <= testedPairIndex
                                              && ((firstShouldWork && cachedMethodIndex == 0)
                                                  || (othersShouldWork && cachedMethodIndex > 0));
                    }

                    if ( shouldBeInvalidated )
                    {
                        AssertEx.NotEqual(
                            cachedMethodValuesBeforeInvalidation[cachedMethodIndex],
                            cachedMethodValueAfterInvalidation,
                            $"{testDescription}: The value after invalidation of the invalidated method #{cachedMethodIndex} is the same as the cached one when invalidating by the method #{testedPairIndex}." );
                    }
                    else
                    {
                        AssertEx.Equal(
                            cachedMethodValuesBeforeInvalidation[cachedMethodIndex],
                            cachedMethodValueAfterInvalidation,
                            $"{testDescription}: The value after invalidation of the not invalidated method #{cachedMethodIndex} is different than the cached one when invalidating by the method #{testedPairIndex}." );
                    }
                }
            }
        }

        protected InvalidationTestsBase( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }
    }
}