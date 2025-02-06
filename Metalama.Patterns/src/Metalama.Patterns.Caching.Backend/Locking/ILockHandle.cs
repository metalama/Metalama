// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Caching.Locking;

/// <summary>
/// Allows to acquire and release a named lock returned by <see cref="ILockingStrategy"/>.
/// </summary>
public interface ILockHandle : IDisposable
{
    /// <summary>
    /// Synchronously acquires the lock bound to the current handle.
    /// </summary>
    /// <param name="timeout">Timeout.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns><c>true</c> if the lock was acquired, or <c>false</c> if the operation has timed out before the lock could be acquired.</returns>
    bool Acquire( TimeSpan timeout, CancellationToken cancellationToken );

    /// <summary>
    /// Asynchronously acquires the lock bound to the current handle.
    /// </summary>
    /// <param name="timeout">Timeout.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns><c>true</c> if the lock was acquired, or <c>false</c> if the operation has timed out before the lock could be acquired.</returns>
    ValueTask<bool> AcquireAsync( TimeSpan timeout, CancellationToken cancellationToken );

    /// <summary>
    /// Synchronously releases the lock bound to the current handle.
    /// </summary>
    void Release();

    /// <summary>
    /// Asynchronously releases the lock bound to the current handle.
    /// </summary>
    ValueTask ReleaseAsync();
}