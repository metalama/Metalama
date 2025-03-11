// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Locking;

/// <summary>
/// An implementation of <see cref="ILockingStrategy"/> which does not acquire any lock.
/// </summary>
[PublicAPI]
public class NullLockingStrategy : ILockingStrategy
{
    /// <inheritdoc />
    public ILockHandle GetLock( string key ) => LockHandle.Instance;

    private class LockHandle : ILockHandle
    {
        public static readonly LockHandle Instance = new();

        public ValueTask ReleaseAsync() => default;

        public bool Acquire( TimeSpan timeout, CancellationToken cancellationToken ) => true;

        public ValueTask<bool> AcquireAsync( TimeSpan timeout, CancellationToken cancellationToken ) => new( true );

        public void Release() { }

        public void Dispose() { }
    }
}