// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Locking;

/// <summary>
/// Provides instances of named locks for synchronizing concurrent executions of cached methods.
/// </summary>
/// <remarks>
/// <para>Two built-in implementations are provided:</para>
/// <list type="bullet">
/// <item><description><see cref="NullLockingStrategy"/>: Allows concurrent execution (default).</description></item>
/// <item><description><see cref="LocalLockingStrategy"/>: Prevents concurrent execution of the same method with the same parameters in the current process.</description></item>
/// </list>
/// <para>The locking strategy is configured per caching profile through the
/// <c>CachingProfile.LockingStrategy</c> property.</para>
/// </remarks>
/// <seealso cref="ILockHandle"/>
/// <seealso cref="NullLockingStrategy"/>
/// <seealso cref="LocalLockingStrategy"/>
public interface ILockingStrategy
{
    /// <summary>
    /// Gets a handle to a named lock. This method must return immediately. Waiting, if any, must be done in the <see cref="ILockHandle.Acquire"/> method.
    /// </summary>
    /// <param name="key">The name of the lock.</param>
    /// <returns>A handle to the lock named <paramref name="key"/>.</returns>
    ILockHandle GetLock( string key );
}