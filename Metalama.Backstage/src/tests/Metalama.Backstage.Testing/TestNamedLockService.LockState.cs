// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Backstage.Testing
{
    public sealed partial class TestNamedLockService
    {
        /// <summary>
        /// The state of one named lock. Every member is guarded by the monitor of the service.
        /// </summary>
        private sealed class LockState
        {
            /// <summary>
            /// Gets or sets the managed identifier of the thread that owns the lock, or <see langword="null"/> if
            /// no thread of this process owns it.
            /// </summary>
            public int? OwnerThreadId { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the lock is held by <see cref="Pin"/>, which stands for
            /// another process and therefore belongs to no thread of this one.
            /// </summary>
            public bool IsPinned { get; set; }

            /// <summary>
            /// Gets or sets the number of subsequent acquisitions that must fail as if the timeout had elapsed.
            /// </summary>
            public int ForcedTimeouts { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the next acquisition must report that the previous owner
            /// terminated without releasing the lock.
            /// </summary>
            public bool IsAbandoned { get; set; }

            /// <summary>
            /// Gets or sets the number of times the lock has been acquired from a previous owner that terminated
            /// without releasing it.
            /// </summary>
            /// <remarks>
            /// A test asserts on this rather than only on the acquisition having succeeded. Without it, a test of
            /// the abandonment path cannot be distinguished from a test that acquires a lock nobody held, which is
            /// what this class used to make it: <see cref="Abandon"/> set <see cref="IsAbandoned"/> and the
            /// acquisition never read it.
            /// </remarks>
            public int AbandonedAcquisitionCount { get; set; }

            /// <summary>
            /// Gets or sets a factory of the exception that the next acquisition must throw, or
            /// <see langword="null"/>.
            /// </summary>
            public Func<Exception>? ArmedException { get; set; }

            /// <summary>
            /// Gets or sets the number of times the lock has been acquired.
            /// </summary>
            public int AcquisitionCount { get; set; }

            /// <summary>
            /// Gets or sets the number of times the lock has been created by
            /// <see cref="Metalama.Backstage.Threading.INamedLockService"/>.
            /// </summary>
            public int CreationCount { get; set; }
        }
    }
}
