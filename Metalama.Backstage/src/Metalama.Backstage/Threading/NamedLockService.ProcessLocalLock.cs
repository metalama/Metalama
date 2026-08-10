// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading;

namespace Metalama.Backstage.Threading
{
    public sealed partial class NamedLockService
    {
        /// <summary>
        /// A lock backed by a monitor of the current process, used when the operating system cannot provide a
        /// named object.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the degraded mode described in the remarks of <see cref="INamedLockService.GetLock"/>. It
        /// excludes the threads of the current process but not the other processes of the machine. It exists
        /// because losing mutual exclusion between processes is a much better outcome than failing the build,
        /// which is what used to happen on Unix when the directory backing the named objects was unusable.
        /// </para>
        /// <para>
        /// The monitor is owned by the service and shared by every lock of the same name, so that two locks
        /// obtained separately for the same name still exclude each other.
        /// </para>
        /// </remarks>
        private sealed class ProcessLocalLock : NamedLockBase
        {
            private readonly SemaphoreSlim _monitor;

            /// <summary>
            /// Initializes a new instance of the <see cref="ProcessLocalLock"/> class.
            /// </summary>
            /// <param name="service">The service that created this lock.</param>
            /// <param name="name">The name of the lock.</param>
            /// <param name="monitor">The monitor shared by every lock of this name, owned by the service.</param>
            public ProcessLocalLock( NamedLockService service, string name, SemaphoreSlim monitor ) : base( service, name )
            {
                this._monitor = monitor;
            }

            /// <inheritdoc />
            protected override bool TryAcquireCore( TimeSpan timeout, CancellationToken cancellationToken, out bool wasAbandoned )
            {
                // A monitor of the current process cannot be abandoned: the process that owns it is the process
                // that would have to terminate for the question to arise.
                wasAbandoned = false;

                // SemaphoreSlim.Wait is natively cancellable and throws OperationCanceledException, which is the
                // same contract as the operating system implementation above.
                return this._monitor.Wait( timeout, cancellationToken );
            }

            /// <inheritdoc />
            protected override void ReleaseCore() => this._monitor.Release();

            /// <inheritdoc />
            public override void Dispose()
            {
                // The monitor belongs to the service, which shares it with the other locks of the same name, so
                // this object owns no resource of its own.
            }
        }
    }
}
