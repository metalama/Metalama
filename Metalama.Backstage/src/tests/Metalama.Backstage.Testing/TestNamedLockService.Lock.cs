// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Threading;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Metalama.Backstage.Testing
{
    public sealed partial class TestNamedLockService
    {
        /// <summary>
        /// A named lock backed by the monitor of the service rather than by an operating system object.
        /// </summary>
        private sealed class Lock : INamedLock
        {
            private readonly TestNamedLockService _service;

            /// <summary>
            /// Initializes a new instance of the <see cref="Lock"/> class.
            /// </summary>
            /// <param name="service">The service that created this lock.</param>
            /// <param name="name">The name of the lock.</param>
            public Lock( TestNamedLockService service, string name )
            {
                this._service = service;
                this.Name = name;
            }

            /// <inheritdoc />
            public string Name { get; }

            /// <inheritdoc />
            public bool TryAcquire( TimeSpan timeout, [NotNullWhen( true )] out IDisposable? releaser, CancellationToken cancellationToken = default )
            {
                releaser = null;

                cancellationToken.ThrowIfCancellationRequested();

                // A timeout of zero is a request not to wait at all, rather than a duration, so the override never
                // applies to it. Overriding it would turn every non-blocking probe of the code under test into an
                // unbounded wait.
                var effectiveTimeout = timeout == TimeSpan.Zero ? TimeSpan.Zero : this._service.TimeoutOverride ?? timeout;
                var threadId = Environment.CurrentManagedThreadId;

                lock ( this._service._sync )
                {
                    var state = this._service.GetOrCreateState( this.Name );

                    if ( state.ArmedException != null )
                    {
                        var exceptionFactory = state.ArmedException;
                        state.ArmedException = null;

                        throw exceptionFactory();
                    }

                    if ( state.ForcedTimeouts > 0 )
                    {
                        state.ForcedTimeouts--;
                        this._service.Log( $"TryAcquire '{this.Name}': forced timeout." );

                        return false;
                    }

                    this._service.VerifyDiscipline( this.Name );

                    var deadline = effectiveTimeout == Timeout.InfiniteTimeSpan
                        ? (DateTime?) null
                        : DateTime.UtcNow + effectiveTimeout;

                    while ( state.OwnerThreadId != null || state.IsPinned )
                    {
                        this._service._nameWaitedForByThread[threadId] = this.Name;

                        try
                        {
                            this._service.DetectDeadlock( this.Name );
                            this._service.SignalWaiterCountWithinLock( this.Name );

                            var remaining = deadline == null
                                ? Timeout.Infinite
                                : (int) Math.Max( 0, (deadline.Value - DateTime.UtcNow).TotalMilliseconds );

                            if ( !Monitor.Wait( this._service._sync, remaining ) )
                            {
                                this._service.Log( $"TryAcquire '{this.Name}': timed out." );

                                return false;
                            }
                        }
                        finally
                        {
                            this._service._nameWaitedForByThread.Remove( threadId );
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    state.OwnerThreadId = threadId;
                    state.AcquisitionCount++;
                    state.IsAbandoned = false;

                    if ( !this._service._namesHeldByThread.TryGetValue( threadId, out var held ) )
                    {
                        held = new System.Collections.Generic.List<string>();
                        this._service._namesHeldByThread.Add( threadId, held );
                    }

                    held.Add( this.Name );

                    this._service.Log( $"TryAcquire '{this.Name}': acquired by thread {threadId}." );

                    releaser = new Releaser( this._service, this.Name, threadId );

                    return true;
                }
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

            /// <inheritdoc />
            public void Dispose()
            {
                // The state belongs to the service and is shared with the other locks of the same name, so this
                // object owns no resource of its own.
            }
        }

        /// <summary>
        /// Releases an acquisition made by <see cref="Lock.TryAcquire"/>.
        /// </summary>
        private sealed class Releaser : IDisposable
        {
            private readonly TestNamedLockService _service;
            private readonly string _name;
            private readonly int _threadId;
            private int _isDisposed;

            /// <summary>
            /// Initializes a new instance of the <see cref="Releaser"/> class.
            /// </summary>
            /// <param name="service">The service.</param>
            /// <param name="name">The name of the lock.</param>
            /// <param name="threadId">The managed identifier of the thread that acquired the lock.</param>
            public Releaser( TestNamedLockService service, string name, int threadId )
            {
                this._service = service;
                this._name = name;
                this._threadId = threadId;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                if ( Interlocked.Exchange( ref this._isDisposed, 1 ) != 0 )
                {
                    return;
                }

                lock ( this._service._sync )
                {
                    // A named lock has thread affinity, so releasing one from another thread is a defect that the
                    // operating system implementation would report as an ApplicationException.
                    if ( Environment.CurrentManagedThreadId != this._threadId )
                    {
                        this._service.Fail(
                            $"The lock '{this._name}' was acquired by thread {this._threadId} and is released by thread "
                            + $"{Environment.CurrentManagedThreadId}. Named locks have thread affinity." );
                    }

                    var state = this._service.GetOrCreateState( this._name );
                    state.OwnerThreadId = null;

                    if ( this._service._namesHeldByThread.TryGetValue( this._threadId, out var held ) )
                    {
                        held.Remove( this._name );
                    }

                    this._service.Log( $"Release '{this._name}' by thread {this._threadId}." );

                    Monitor.PulseAll( this._service._sync );
                }
            }
        }

        /// <summary>
        /// Releases a lock held by <see cref="Pin"/>.
        /// </summary>
        private sealed class PinHandle : IDisposable
        {
            private readonly TestNamedLockService _service;
            private readonly string _name;
            private int _isDisposed;

            /// <summary>
            /// Initializes a new instance of the <see cref="PinHandle"/> class.
            /// </summary>
            /// <param name="service">The service.</param>
            /// <param name="name">The name of the lock.</param>
            public PinHandle( TestNamedLockService service, string name )
            {
                this._service = service;
                this._name = name;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                if ( Interlocked.Exchange( ref this._isDisposed, 1 ) != 0 )
                {
                    return;
                }

                lock ( this._service._sync )
                {
                    this._service.GetOrCreateState( this._name ).IsPinned = false;
                    this._service.Log( $"Unpin '{this._name}'." );

                    Monitor.PulseAll( this._service._sync );
                }
            }
        }
    }
}
