// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using System.Diagnostics;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency
{
    public sealed partial class LayeredCachingBackendEnhancerConcurrencyTests
    {
        /// <summary>
        /// A single-use gate armed on a <see cref="RemoteBackendDouble"/>. The gate blocks the thread that reads the item
        /// of a chosen key from the double until the test calls <see cref="Release"/>. The thread is blocked after the
        /// double has read its store and before the read returns.
        /// </summary>
        /// <remarks>
        /// The double releases its own lock before it calls the gate, so a thread that is blocked at the gate holds no
        /// lock of the double. The gate waits on a monitor, so it owns no disposable resource.
        /// </remarks>
        private sealed class RemoteReadGate
        {
            /// <summary>
            /// The object that protects the state of the gate and on which the threads wait.
            /// </summary>
            private readonly object _sync = new();

            /// <summary>
            /// The key whose read the gate blocks.
            /// </summary>
            private readonly string _key;

            /// <summary>
            /// A value indicating whether a read has reached the gate.
            /// </summary>
            private bool _isReached;

            /// <summary>
            /// A value indicating whether the test has released the gate.
            /// </summary>
            private bool _isReleased;

            /// <summary>
            /// Initializes a new instance of the <see cref="RemoteReadGate"/> class.
            /// </summary>
            /// <param name="key">The key whose read the gate blocks.</param>
            public RemoteReadGate( string key )
            {
                this._key = key;
            }

            /// <summary>
            /// Gets the name of the thread that reached the gate, or <see langword="null"/> when no thread has reached it.
            /// </summary>
            public string? ReachedThreadName { get; private set; }

            /// <summary>
            /// Gets the item that the blocked read returns, or <see langword="null"/> when no thread has reached the gate or
            /// when the double holds no item for the key.
            /// </summary>
            public CacheItem? ReadItem { get; private set; }

            /// <summary>
            /// Called by the <see cref="RemoteBackendDouble"/> for each read. Blocks the calling thread when the read is
            /// the first read of the key of the gate.
            /// </summary>
            /// <param name="key">The key that is read.</param>
            /// <param name="readItem">The item that the read returns.</param>
            internal void OnRead( string key, CacheItem? readItem )
            {
                if ( !string.Equals( key, this._key, StringComparison.Ordinal ) )
                {
                    return;
                }

                lock ( this._sync )
                {
                    if ( this._isReached )
                    {
                        return;
                    }

                    this._isReached = true;
                    this.ReachedThreadName = Thread.CurrentThread.Name;
                    this.ReadItem = readItem;
                    Monitor.PulseAll( this._sync );

                    while ( !this._isReleased )
                    {
                        Monitor.Wait( this._sync );
                    }
                }
            }

            /// <summary>
            /// Waits until a read has reached the gate. The timeout only detects a failure of the test.
            /// </summary>
            /// <param name="timeout">The maximum time to wait.</param>
            /// <returns><see langword="true"/> when a read has reached the gate, otherwise <see langword="false"/>.</returns>
            public bool WaitUntilReached( TimeSpan timeout )
            {
                var stopwatch = Stopwatch.StartNew();

                lock ( this._sync )
                {
                    while ( !this._isReached )
                    {
                        var remaining = timeout - stopwatch.Elapsed;

                        if ( remaining <= TimeSpan.Zero )
                        {
                            return false;
                        }

                        Monitor.Wait( this._sync, remaining );
                    }

                    return true;
                }
            }

            /// <summary>
            /// Releases the thread that is blocked at the gate. When no read has reached the gate yet, the gate no longer
            /// blocks the read that reaches it.
            /// </summary>
            public void Release()
            {
                lock ( this._sync )
                {
                    this._isReleased = true;
                    Monitor.PulseAll( this._sync );
                }
            }
        }
    }
}
