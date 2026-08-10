// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
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
        /// A lock backed by a named <see cref="Mutex"/>, and therefore shared by all the processes of the machine.
        /// This is the normal case.
        /// </summary>
        private sealed class OperatingSystemLock : NamedLockBase
        {
            private readonly Mutex _mutex;

            /// <summary>
            /// Initializes a new instance of the <see cref="OperatingSystemLock"/> class.
            /// </summary>
            /// <param name="service">The service that created this lock.</param>
            /// <param name="name">The name of the lock.</param>
            /// <param name="mutex">The mutex, whose ownership is transferred to this object.</param>
            public OperatingSystemLock( NamedLockService service, string name, Mutex mutex ) : base( service, name )
            {
                this._mutex = mutex;
            }

            /// <inheritdoc />
            protected override bool TryAcquireCore( TimeSpan timeout, CancellationToken cancellationToken, out bool wasAbandoned )
            {
                wasAbandoned = false;

                try
                {
                    if ( !cancellationToken.CanBeCanceled )
                    {
                        return this._mutex.WaitOne( timeout );
                    }

                    // Mutex.WaitOne has no cancellable overload, so the wait handle of the token is waited upon
                    // alongside the mutex itself. The array is allocated for each wait, which is acceptable
                    // because this path is taken only when the caller actually supplied a token, and only when
                    // the lock was found to be owned.
                    var index = WaitHandle.WaitAny( new WaitHandle[] { this._mutex, cancellationToken.WaitHandle }, timeout );

                    switch ( index )
                    {
                        case 0:
                            return true;

                        case 1:
                            // The token was signalled first, so the mutex was not acquired and must not be
                            // released.
                            throw new OperationCanceledException( cancellationToken );

                        default:
                            // WaitHandle.WaitTimeout.
                            return false;
                    }
                }
                catch ( AbandonedMutexException e )
                {
                    // The previous owner terminated without releasing the mutex. The wait has nonetheless
                    // succeeded and this thread now owns it, so the acquisition is reported as successful and the
                    // caller is told through the event that the protected state may be inconsistent.
                    // WaitAny reports which handle was abandoned, and WaitOne reports -1; only the mutex, which is
                    // at index 0, can ever be the one.
                    if ( e.MutexIndex is 0 or -1 )
                    {
                        wasAbandoned = true;

                        return true;
                    }

                    throw;
                }
            }

            /// <inheritdoc />
            /// <remarks>
            /// A release that finds the handle already closed is not an error. It means that the owner of the lock
            /// was disposed while this acquisition was still in flight, which happens during the teardown of a
            /// process: the state the lock protected is no longer of interest to anybody, and the operating system
            /// has released the mutex along with the handle. Letting the exception escape would instead fail the
            /// operation that was holding the lock, and would do so out of a method documented never to throw.
            /// </remarks>
            protected override void ReleaseCore()
            {
                try
                {
                    this._mutex.ReleaseMutex();
                }
                catch ( ObjectDisposedException )
                {
                    // See the remarks above. There is deliberately nothing to report: the service that would
                    // report it is being torn down as well.
                }
            }

            /// <inheritdoc />
            public override void Dispose() => this._mutex.Dispose();
        }
    }
}
