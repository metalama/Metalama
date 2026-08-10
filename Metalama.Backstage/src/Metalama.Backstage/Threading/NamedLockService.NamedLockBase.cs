// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Metalama.Backstage.Threading
{
#if METALAMA_BACKSTAGE
    public
#else
    internal
#endif
        sealed partial class NamedLockService
    {
        /// <summary>
        /// The part of <see cref="INamedLock"/> that does not depend on the primitive backing the lock, namely
        /// the reentrancy check, the reporting and the release token.
        /// </summary>
        private abstract class NamedLockBase : INamedLock
        {
            private readonly NamedLockService _service;

            /// <summary>
            /// Initializes a new instance of the <see cref="NamedLockBase"/> class.
            /// </summary>
            /// <param name="service">The service that created this lock.</param>
            /// <param name="name">The name of the lock.</param>
            protected NamedLockBase( NamedLockService service, string name )
            {
                this._service = service;
                this.Name = name;
            }

            /// <inheritdoc />
            public string Name { get; }

            /// <summary>
            /// Waits for the underlying primitive.
            /// </summary>
            /// <param name="timeout">The maximal waiting time.</param>
            /// <param name="cancellationToken">A token that aborts the wait.</param>
            /// <param name="wasAbandoned">
            /// At output, whether the previous owner of the lock terminated without releasing it.
            /// </param>
            /// <returns><see langword="true"/> if the lock was acquired.</returns>
            protected abstract bool TryAcquireCore( TimeSpan timeout, CancellationToken cancellationToken, out bool wasAbandoned );

            /// <summary>
            /// Releases the underlying primitive.
            /// </summary>
            protected abstract void ReleaseCore();

            /// <inheritdoc />
            public abstract void Dispose();

            /// <inheritdoc />
            public bool TryAcquire( TimeSpan timeout, [NotNullWhen( true )] out IDisposable? releaser, CancellationToken cancellationToken = default )
            {
                releaser = null;

                cancellationToken.ThrowIfCancellationRequested();

                this._service.CheckNotReentrant( this.Name );

                var startTimestamp = Stopwatch.GetTimestamp();

                // Probe without waiting first, so that the uncontended case, which is by far the most frequent,
                // reports nothing beyond the acquisition itself, and so that a contended one is visible as such.
                // No token is passed: the probe does not wait, so there is nothing to abort, and passing one would
                // send every uncontended acquisition through the allocating WaitHandle.WaitAny path. An
                // already-cancelled token has been rejected above.
                var acquired = this.TryAcquireCore( TimeSpan.Zero, CancellationToken.None, out var wasAbandoned );

                if ( !acquired && timeout != TimeSpan.Zero )
                {
                    this._service.Report( LockEventKind.Blocked, this.Name );

                    // A test pinned here has a thread that is known to be about to wait, and that has not waited
                    // yet, which is a state the Blocked event can report but cannot hold still.
                    this._service.SyncPoint( BeforeWaitLocation, this.Name, cancellationToken );

                    acquired = this.TryAcquireCore( timeout, cancellationToken, out wasAbandoned );
                }

                var waited = GetElapsed( startTimestamp );

                if ( !acquired )
                {
                    this._service.Report( LockEventKind.TimedOut, this.Name, waited );

                    return false;
                }

                MarkHeldByCurrentThread( this.Name );

                // A test pinned here holds the lock without yet having anything with which to release it, which is
                // how the absence of a window between owning the lock and enforcing it is verified. No token is
                // passed, for the same reason as in Release below: the lock is already owned at this point, so
                // aborting here would leak it, the caller never having received anything to release it with.
                this._service.SyncPoint( AfterWaitLocation, this.Name, CancellationToken.None );

                this._service.Report(
                    wasAbandoned ? LockEventKind.Abandoned : LockEventKind.Acquired,
                    this.Name,
                    waited,
                    wasAbandoned ? "the previous owner terminated without releasing the lock" : null );

                releaser = new Releaser( this );

                return true;
            }

            /// <inheritdoc />
            public IDisposable Acquire( TimeSpan? timeout = null, CancellationToken cancellationToken = default )
            {
                var effectiveTimeout = timeout ?? Timeout.InfiniteTimeSpan;

                if ( !this.TryAcquire( effectiveTimeout, out var releaser, cancellationToken ) )
                {
                    throw new TimeoutException( $"Could not acquire the lock '{this.Name}' within {effectiveTimeout}." );
                }

                return releaser;
            }

            /// <summary>
            /// Releases the lock and reports it, including the time it was held.
            /// </summary>
            private void Release()
            {
                // A test pinned here has a lock that is being released but is still owned, which is the only way
                // to assert on the exact boundary of a release without depending on timing. No token is passed,
                // because a release must complete even when the operation that owned the lock was cancelled.
                this._service.SyncPoint( BeforeReleaseLocation, this.Name, CancellationToken.None );

                MarkReleasedByCurrentThread( this.Name );

                this.ReleaseCore();
            }

            /// <summary>
            /// The object returned by <see cref="TryAcquire"/>, whose disposal releases the lock.
            /// </summary>
            /// <remarks>
            /// Unlike the implementation this one replaces, this class has no finalizer and captures no stack
            /// trace. The finalizer of the previous implementation threw in a debug build, which terminates the
            /// process on .NET Core, and a release token is now allocated on every acquisition rather than once
            /// per lock.
            /// </remarks>
            private sealed class Releaser : IDisposable
            {
                private readonly long _acquiredTimestamp = Stopwatch.GetTimestamp();
                private NamedLockBase? _lock;

                /// <summary>
                /// Initializes a new instance of the <see cref="Releaser"/> class.
                /// </summary>
                /// <param name="lock">The lock to release on disposal.</param>
                public Releaser( NamedLockBase @lock )
                {
                    this._lock = @lock;
                }

                /// <inheritdoc />
                public void Dispose()
                {
                    // Disposal is idempotent, and the exchange also guarantees that a second disposal cannot
                    // release a lock that another thread has since acquired.
                    var @lock = Interlocked.Exchange( ref this._lock, null );

                    if ( @lock == null )
                    {
                        return;
                    }

                    @lock.Release();

                    var held = GetElapsed( this._acquiredTimestamp );

                    @lock._service.Report( LockEventKind.Released, @lock.Name, held );

                    if ( held.TotalMilliseconds > _longHoldThresholdMilliseconds )
                    {
                        @lock._service.Report( LockEventKind.HeldTooLong, @lock.Name, held );
                    }
                }
            }
        }
    }
}
