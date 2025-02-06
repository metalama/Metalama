// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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