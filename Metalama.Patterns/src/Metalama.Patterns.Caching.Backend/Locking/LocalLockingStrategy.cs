// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System.Collections.Concurrent;

namespace Metalama.Patterns.Caching.Locking;

/// <summary>
/// An implementation of <see cref="ILockingStrategy"/> in which every instance of the <see cref="LocalLockingStrategy"/>
/// has its own set of named locks that are not shared in any way with other instances. The <see cref="LocalLockingStrategy"/> can
/// be used to synchronize the execution of methods in the current process and <see cref="AppDomain"/>.
/// </summary>
[PublicAPI]
public sealed class LocalLockingStrategy : ILockingStrategy
{
    private readonly ConcurrentDictionary<string, Lock> _locks = new( StringComparer.OrdinalIgnoreCase );

    /// <inheritdoc />
    public ILockHandle GetLock( string key )
    {
        var @lock = this._locks.AddOrUpdate( key, k => new Lock( this, k ), ( _, l ) => l.AddReference() );
#if DEBUG
        if ( @lock.References <= 0 )
        {
            throw new CachingAssertionFailedException();
        }
#endif
        return new LockHandle( @lock );
    }

    private class LockHandle : ILockHandle
    {
        private readonly Lock _lock;
        private bool _disposed;
        private bool _acquired;

        public LockHandle( Lock @lock )
        {
            this._lock = @lock;
        }

        public bool Acquire( TimeSpan timeout, CancellationToken cancellationToken )
        {
            if ( this._acquired )
            {
                throw new InvalidOperationException();
            }

            this._acquired = this._lock.Wait( timeout, cancellationToken );

            return this._acquired;
        }

        public async ValueTask<bool> AcquireAsync( TimeSpan timeout, CancellationToken cancellationToken )
        {
            if ( this._acquired )
            {
                throw new InvalidOperationException();
            }

            this._acquired = await this._lock.WaitAsync( timeout, cancellationToken );

            return this._acquired;
        }

        public void Release()
        {
            if ( this._acquired )
            {
                this._lock.Release();
                this._acquired = false;
            }
        }

        public ValueTask ReleaseAsync()
        {
            this.Release();

            return default;
        }

        public void Dispose()
        {
            if ( this._acquired )
            {
                throw new InvalidOperationException( "The lock is still acquired." );
            }

            if ( !this._disposed )
            {
                this._lock.RemoveReference();
                this._disposed = true;

#if DEBUG
                GC.SuppressFinalize( this );
#endif
            }
        }

#if DEBUG
#pragma warning disable CA1821 // Remove empty Finalizers
        ~LockHandle()
        {
            throw new CachingAssertionFailedException( "The Dispose method has not been invoked." );
        }
#pragma warning restore CA1821 // Remove empty Finalizers
#endif
    }

    private class Lock : SemaphoreSlim
    {
        private readonly LocalLockingStrategy _parent;
        private readonly string _key;

        public int References { get; private set; } = 1;

        // This locks prevents a data race between AddReference and RemoveReference. 
        // It enforces the following invariant: this.References == 0 and 'this' is not present in in this.parent.lock.
        private SpinLock _spinLock;

        public Lock( LocalLockingStrategy parent, string key ) : base( 1 )
        {
            this._parent = parent;
            this._key = key;
        }

        public Lock AddReference()
        {
            var lockTaken = false;

            try
            {
                this._spinLock.Enter( ref lockTaken );
                this.References++;
            }
            finally
            {
                if ( lockTaken )
                {
                    this._spinLock.Exit( true );
                }
            }

            return this;
        }

        public void RemoveReference()
        {
            var lockTaken = false;

            try
            {
                this._spinLock.Enter( ref lockTaken );

                this.References--;

                if ( this.References == 0 )
                {
                    if ( !this._parent._locks.TryRemove( this._key, out var removedLock ) || removedLock != this )
                    {
                        throw new CachingAssertionFailedException( "Data race." );
                    }
                }
            }
            finally
            {
                if ( lockTaken )
                {
                    this._spinLock.Exit( true );
                }
            }
        }
    }
}