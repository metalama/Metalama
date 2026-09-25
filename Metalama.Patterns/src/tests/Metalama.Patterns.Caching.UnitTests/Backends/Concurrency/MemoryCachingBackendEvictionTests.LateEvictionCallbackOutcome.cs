// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency
{
    public sealed partial class MemoryCachingBackendEvictionTests
    {
        /// <summary>
        /// The observations of <see cref="RunLateEvictionCallbackScheduleAsync"/> that the tests assert.
        /// </summary>
        private sealed class LateEvictionCallbackOutcome
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="LateEvictionCallbackOutcome"/> class.
            /// </summary>
            /// <param name="dependencyBeforeCallback">The value of <see cref="DependencyBeforeCallback"/>.</param>
            /// <param name="eventsAfterSecondSet">The value of <see cref="EventsAfterSecondSet"/>.</param>
            /// <param name="itemAfterCallback">The value of <see cref="ItemAfterCallback"/>.</param>
            /// <param name="dependencyAfterCallback">The value of <see cref="DependencyAfterCallback"/>.</param>
            /// <param name="itemAfterInvalidation">The value of <see cref="ItemAfterInvalidation"/>.</param>
            public LateEvictionCallbackOutcome(
                bool dependencyBeforeCallback,
                IReadOnlyList<CacheItemRemovedEventArgs> eventsAfterSecondSet,
                CacheItem? itemAfterCallback,
                bool dependencyAfterCallback,
                CacheItem? itemAfterInvalidation )
            {
                this.DependencyBeforeCallback = dependencyBeforeCallback;
                this.EventsAfterSecondSet = eventsAfterSecondSet;
                this.ItemAfterCallback = itemAfterCallback;
                this.DependencyAfterCallback = dependencyAfterCallback;
                this.ItemAfterInvalidation = itemAfterInvalidation;
            }

            /// <summary>
            /// Gets a value indicating whether the dependency set existed after the second value was stored and before the
            /// late callback ran.
            /// </summary>
            public bool DependencyBeforeCallback { get; }

            /// <summary>
            /// Gets the <see cref="CachingBackend.ItemRemoved"/> events that were raised after the second value was stored and
            /// before the dependency was invalidated.
            /// </summary>
            public IReadOnlyList<CacheItemRemovedEventArgs> EventsAfterSecondSet { get; }

            /// <summary>
            /// Gets the item that <see cref="CachingBackend.GetItem"/> returned after the late callback ran.
            /// </summary>
            public CacheItem? ItemAfterCallback { get; }

            /// <summary>
            /// Gets a value indicating whether the dependency set existed after the late callback ran.
            /// </summary>
            public bool DependencyAfterCallback { get; }

            /// <summary>
            /// Gets the item that <see cref="CachingBackend.GetItem"/> returned after the dependency was invalidated.
            /// </summary>
            public CacheItem? ItemAfterInvalidation { get; }
        }
    }
}
