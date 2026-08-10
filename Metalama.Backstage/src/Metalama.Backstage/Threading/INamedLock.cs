// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Metalama.Backstage.Threading;

/// <summary>
/// Represents a named lock created by <see cref="INamedLockService.GetLock"/>.
/// </summary>
/// <remarks>
/// <para>
/// Disposing this object releases the operating system resources backing the lock. It does not release the lock
/// itself, which is released by disposing the object returned by <see cref="TryAcquire"/>.
/// </para>
/// <para>
/// The lock has thread affinity, in the same way as <see cref="System.Threading.Mutex"/>. The thread that
/// acquires the lock must be the thread that releases it, therefore the lock must never be held across an
/// <c>await</c>.
/// </para>
/// </remarks>
[PublicAPI]
#if METALAMA_BACKSTAGE
public
#else

// See the remark on the accessibility of INamedLockService.
internal
#endif
    interface INamedLock : IDisposable
{
    /// <summary>
    /// Gets the name of the lock, as it was given to <see cref="INamedLockService.GetLock"/>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Attempts to acquire the lock, waiting for at most a given time.
    /// </summary>
    /// <param name="timeout">
    /// The maximal waiting time. Pass <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> to wait
    /// indefinitely, or <see cref="TimeSpan.Zero"/> to return immediately when the lock is owned.
    /// </param>
    /// <param name="releaser">
    /// At output, an object that releases the lock when it is disposed, or <see langword="null"/> if the lock was
    /// not acquired.
    /// </param>
    /// <param name="cancellationToken">A token that aborts the wait.</param>
    /// <returns>
    /// <see langword="true"/> if the lock was acquired, or <see langword="false"/> if it could not be acquired
    /// within <paramref name="timeout"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> was cancelled, in which case the lock was not acquired. This is the
    /// only circumstance in which this method throws, and it is a request made by the caller itself.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method does not throw when the lock cannot be acquired within the timeout, because the decision
    /// whether to fail, to degrade or to skip the operation belongs to the caller and differs from one caller to
    /// another.
    /// </para>
    /// <para>
    /// If the previous owner of the lock terminated without releasing it, this method acquires the lock and
    /// returns normally, and reports a <see cref="LockEventKind.Abandoned"/> event. The state protected by the
    /// lock may then be inconsistent, and validating it is the responsibility of the caller.
    /// </para>
    /// <para>
    /// The lock is not reentrant. A thread that holds the lock must not attempt to acquire it again, whether
    /// through the same instance or through another instance representing the same name. Reentrancy is a property
    /// of the underlying primitive rather than of this contract, so an implementation backed by a semaphore
    /// deadlocks where an implementation backed by a mutex does not. A reentrant acquisition therefore reports a
    /// <see cref="LockEventKind.ReentrancyDetected"/> event and, in a debug build, throws
    /// <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    bool TryAcquire( TimeSpan timeout, [NotNullWhen( true )] out IDisposable? releaser, CancellationToken cancellationToken = default );

    /// <summary>
    /// Acquires the lock, waiting for at most a given time, and throws if it cannot be acquired.
    /// </summary>
    /// <param name="timeout">
    /// The maximal waiting time, or <see langword="null"/> to wait indefinitely, which is the usual case and the
    /// one in which this method cannot fail.
    /// </param>
    /// <param name="cancellationToken">A token that aborts the wait.</param>
    /// <returns>An object that releases the lock when it is disposed. Never <see langword="null"/>.</returns>
    /// <exception cref="TimeoutException">The lock could not be acquired within <paramref name="timeout"/>.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    /// <remarks>
    /// This is the form to use when the caller has no meaningful way to proceed without the lock, which spares it
    /// from writing the same unreachable failure branch that an unbounded <see cref="TryAcquire"/> would require.
    /// A caller that can proceed without the lock, or that wants to skip the operation instead of failing, uses
    /// <see cref="TryAcquire"/>.
    /// </remarks>
    IDisposable Acquire( TimeSpan? timeout = null, CancellationToken cancellationToken = default );
}
