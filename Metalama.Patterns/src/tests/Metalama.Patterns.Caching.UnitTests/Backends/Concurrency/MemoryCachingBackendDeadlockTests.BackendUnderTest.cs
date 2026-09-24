// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Metalama.Patterns.Caching.Tests.Implementation;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using ITestSynchronizationProvider = Metalama.Testing.Hooks.ITestSynchronizationProvider;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency
{
    public sealed partial class MemoryCachingBackendDeadlockTests
    {
        /// <summary>
        /// Holds a <see cref="MemoryCachingBackend"/> that stores its entries in an <see cref="InterceptingMemoryCache"/>
        /// over a new <see cref="Microsoft.Extensions.Caching.Memory.MemoryCache"/>, gives the test control of the
        /// synchronization points of the backend, and records the events that the backend raises.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The backend resolves its work-item dispatcher, its clock and its <see cref="ITestSynchronizationProvider"/>
        /// from a <see cref="FakeCachingServices"/>. It therefore dispatches its events through a
        /// <see cref="TestWorkItemDispatcher"/>, so a test can wait until every event has been delivered, and it blocks at
        /// the synchronization points that the test arms on <see cref="Synchronization"/>. The backend uses the explicit
        /// <see cref="InterceptingMemoryCache"/> and ignores the memory cache that the service provider registers.
        /// </para>
        /// <para>
        /// The backend does not own the <see cref="InterceptingMemoryCache"/>, because the cache is passed to its
        /// constructor. <see cref="Dispose"/> therefore disposes the cache explicitly, which releases every gate that is
        /// still armed.
        /// </para>
        /// </remarks>
        private sealed class BackendUnderTest : IDisposable
        {
            /// <summary>
            /// The services from which the backend resolves its work-item dispatcher, its clock and its synchronization
            /// provider.
            /// </summary>
            private readonly FakeCachingServices _services;

            /// <summary>
            /// The <see cref="CachingBackend.ItemRemoved"/> events delivered so far, formatted by
            /// <see cref="FormatItemRemovedEvent"/>, in the order of delivery.
            /// </summary>
            private readonly ConcurrentQueue<string> _itemRemovedEvents = new();

            /// <summary>
            /// The keys of the <see cref="CachingBackend.DependencyInvalidated"/> events delivered so far, in the order of
            /// delivery.
            /// </summary>
            private readonly ConcurrentQueue<string> _dependencyInvalidatedEvents = new();

            /// <summary>
            /// Initializes a new instance of the <see cref="BackendUnderTest"/> class. The backend is initialized and its
            /// events are recorded.
            /// </summary>
            public BackendUnderTest()
            {
                this.Synchronization = new TestSynchronizationProvider();
                this._services = new FakeCachingServices( configureServices: s => s.AddSingleton<ITestSynchronizationProvider>( this.Synchronization ) );
                this.Cache = new InterceptingMemoryCache();

                this.Backend = new MemoryCachingBackend(
                    this.Cache,
                    new MemoryCachingBackendConfiguration { DebugName = "test" },
                    this._services.ServiceProvider );

                this.Backend.Initialize();

                this.Backend.ItemRemoved += ( _, args ) => this._itemRemovedEvents.Enqueue( FormatItemRemovedEvent( args.Key, args.RemovedReason ) );
                this.Backend.DependencyInvalidated += ( _, args ) => this._dependencyInvalidatedEvents.Enqueue( args.Key );
            }

            /// <summary>
            /// Gets the cache in which the backend stores its entries. A test arms its gates on this cache.
            /// </summary>
            public InterceptingMemoryCache Cache { get; }

            /// <summary>
            /// Gets the synchronization provider of the backend. A test arms the synchronization points of the backend on
            /// this provider.
            /// </summary>
            public TestSynchronizationProvider Synchronization { get; }

            /// <summary>
            /// Gets the clock of the backend, from which a test computes the expiration of a replacement value.
            /// </summary>
            public TimeProvider TimeProvider => this._services.TimeProvider;

            /// <summary>
            /// Gets the backend under test.
            /// </summary>
            public MemoryCachingBackend Backend { get; }

            /// <summary>
            /// Formats an <see cref="CachingBackend.ItemRemoved"/> event as the record of this class stores it.
            /// </summary>
            /// <param name="key">The key of the removed item.</param>
            /// <param name="reason">The reason of the removal.</param>
            /// <returns>The key and the reason, separated by a colon.</returns>
            public static string FormatItemRemovedEvent( string key, CacheItemRemovedReason reason ) => key + ":" + reason;

            /// <summary>
            /// Waits until the backend has delivered every event that it has raised so far.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> when every event has been delivered within the timeout, otherwise
            /// <see langword="false"/>.
            /// </returns>
            public async Task<bool> WhenEventsDeliveredAsync()
            {
                using var cancellationTokenSource = new CancellationTokenSource( _timeout );

                try
                {
                    await this._services.WhenPendingWorkItemsCompletedAsync( cancellationTokenSource.Token );

                    return true;
                }
                catch ( OperationCanceledException )
                {
                    return false;
                }
            }

            /// <summary>
            /// Gets the <see cref="CachingBackend.ItemRemoved"/> events delivered so far, formatted by
            /// <see cref="FormatItemRemovedEvent"/> and sorted in ordinal order.
            /// </summary>
            /// <returns>The sorted events.</returns>
            /// <remarks>
            /// The backend dispatches each event to the thread pool, so the order of delivery is not specified.
            /// </remarks>
            public string[] GetItemRemovedEvents() => this._itemRemovedEvents.OrderBy( e => e, StringComparer.Ordinal ).ToArray();

            /// <summary>
            /// Gets the keys of the <see cref="CachingBackend.DependencyInvalidated"/> events delivered so far, sorted in
            /// ordinal order.
            /// </summary>
            /// <returns>The sorted keys.</returns>
            /// <remarks>
            /// The backend dispatches each event to the thread pool, so the order of delivery is not specified.
            /// </remarks>
            public string[] GetDependencyInvalidatedEvents() => this._dependencyInvalidatedEvents.OrderBy( e => e, StringComparer.Ordinal ).ToArray();

            /// <summary>
            /// Releases every armed synchronization point, disposes the backend, then disposes the cache, which releases
            /// every armed gate, and finally disposes the services.
            /// </summary>
            public void Dispose()
            {
                this.Synchronization.Dispose();
                this.Backend.Dispose();
                this.Cache.Dispose();
                this._services.Dispose();
            }
        }
    }
}
