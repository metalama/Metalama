// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Caching.Locking;

/// <summary>
/// Provides instances of named locks. 
/// </summary>
/// <remarks>
/// Two implementations are provided: <see cref="NullLockingStrategy"/> and <see cref="LocalLockingStrategy"/>.
/// </remarks>
public interface ILockingStrategy
{
    /// <summary>
    /// Gets a handle to a named lock. This method must return immediately. Waiting, if any, must be done in the <see cref="ILockHandle.Acquire"/> method.
    /// </summary>
    /// <param name="key">The name of the lock.</param>
    /// <returns>A handle to the lock named <paramref name="key"/>.</returns>
    ILockHandle GetLock( string key );
}