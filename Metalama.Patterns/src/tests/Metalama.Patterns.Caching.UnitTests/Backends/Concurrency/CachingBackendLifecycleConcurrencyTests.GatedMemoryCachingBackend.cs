// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Microsoft.Extensions.Caching.Memory;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency
{
    public sealed partial class CachingBackendLifecycleConcurrencyTests
    {
        /// <summary>
        /// A <see cref="MemoryCachingBackend"/> whose asynchronous initialization and synchronous disposal wait at gates
        /// that the test opens.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The initialization gate keeps the backend in the <see cref="CachingBackendStatus.Initializing"/> status, and
        /// the disposal gate keeps it in the <see cref="CachingBackendStatus.Disposing"/> status, for as long as the test
        /// needs. Each gate completes a task when a thread reaches it, so that the test can wait for that moment.
        /// </para>
        /// <para>
        /// Only <see cref="CachingBackend.InitializeAsync"/> and <see cref="CachingBackend.Dispose()"/> reach a gate. The
        /// synchronous initialization and the asynchronous disposal do not wait.
        /// </para>
        /// </remarks>
        private sealed class GatedMemoryCachingBackend : MemoryCachingBackend
        {
            /// <summary>
            /// The source of the task that completes when the initialization reaches its gate.
            /// </summary>
            private readonly TaskCompletionSource<bool> _initializationReached = new( TaskCreationOptions.RunContinuationsAsynchronously );

            /// <summary>
            /// The source of the task that the initialization waits for. The test completes it to open the gate.
            /// </summary>
            private readonly TaskCompletionSource<bool> _initializationGate = new( TaskCreationOptions.RunContinuationsAsynchronously );

            /// <summary>
            /// The source of the task that completes when the synchronous disposal reaches its gate.
            /// </summary>
            private readonly TaskCompletionSource<bool> _disposalReached = new( TaskCreationOptions.RunContinuationsAsynchronously );

            /// <summary>
            /// The source of the task that the synchronous disposal waits for. The test completes it to open the gate.
            /// </summary>
            private readonly TaskCompletionSource<bool> _disposalGate = new( TaskCreationOptions.RunContinuationsAsynchronously );

            /// <summary>
            /// Initializes a new instance of the <see cref="GatedMemoryCachingBackend"/> class. The backend stores its
            /// entries in a new <see cref="MemoryCache"/>, which it disposes when it is disposed.
            /// </summary>
            public GatedMemoryCachingBackend()
                : base( new MemoryCache( new MemoryCacheOptions() ), new MemoryCachingBackendConfiguration { DebugName = "gated" } ) { }

            /// <summary>
            /// Gets a task that completes when the asynchronous initialization reaches its gate. At that moment, the
            /// initialization holds the initialization semaphore, and the status is
            /// <see cref="CachingBackendStatus.Initializing"/>.
            /// </summary>
            public Task InitializationReached => this._initializationReached.Task;

            /// <summary>
            /// Gets a task that completes when the synchronous disposal reaches its gate. At that moment, the status is
            /// <see cref="CachingBackendStatus.Disposing"/>.
            /// </summary>
            public Task DisposalReached => this._disposalReached.Task;

            /// <summary>
            /// Opens the initialization gate. Calling this method more than once has no further effect.
            /// </summary>
            public void OpenInitializationGate() => this._initializationGate.TrySetResult( true );

            /// <summary>
            /// Opens the disposal gate. Calling this method more than once has no further effect.
            /// </summary>
            public void OpenDisposalGate() => this._disposalGate.TrySetResult( true );

            /// <summary>
            /// Signals that the initialization has reached its gate, then waits until the test opens the gate.
            /// </summary>
            /// <param name="cancellationToken">A token that cancels the wait.</param>
            /// <returns>A task that completes when the initialization of the base class has completed.</returns>
            protected override async Task InitializeCoreAsync( CancellationToken cancellationToken = default )
            {
                this._initializationReached.TrySetResult( true );

                if ( !await WaitForCompletionAsync( this._initializationGate.Task, cancellationToken ) )
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await base.InitializeCoreAsync( cancellationToken );
            }

            /// <summary>
            /// Signals that the synchronous disposal has reached its gate, waits until the test opens the gate, then
            /// disposes the backend.
            /// </summary>
            /// <param name="disposing">
            /// <see langword="true"/> when the method is called by <see cref="CachingBackend.Dispose()"/>, or
            /// <see langword="false"/> when the object is being finalized.
            /// </param>
            /// <param name="cancellationToken">A token that cancels the wait and the disposal.</param>
            protected override void DisposeCore( bool disposing, CancellationToken cancellationToken )
            {
                this._disposalReached.TrySetResult( true );
                this._disposalGate.Task.Wait( cancellationToken );

                base.DisposeCore( disposing, cancellationToken );
            }
        }
    }
}
