// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Concurrent;

namespace Metalama.Patterns.Caching.Tests
{
    internal sealed class AsyncBarrier
    {
        private readonly int _participantCount;
        private int _remainingParticipants;
        private ConcurrentStack<TaskCompletionSource<bool>> _waiters;

        public AsyncBarrier( int participantCount )
        {
            if ( participantCount <= 0 )
            {
                throw new ArgumentOutOfRangeException( nameof(participantCount) );
            }

            this._remainingParticipants = this._participantCount = participantCount;
            this._waiters = new ConcurrentStack<TaskCompletionSource<bool>>();
        }

        public Task SignalAndWait()
        {
            Console.WriteLine( "SignalAndWait" );
            var tcs = new TaskCompletionSource<bool>();
            this._waiters.Push( tcs );

            if ( Interlocked.Decrement( ref this._remainingParticipants ) == 0 )
            {
                this._remainingParticipants = this._participantCount;
                var waiters = this._waiters;
                this._waiters = new ConcurrentStack<TaskCompletionSource<bool>>();
                Parallel.ForEach( waiters, w => w.SetResult( true ) );
            }

            return tcs.Task;
        }

        public override string ToString()
        {
            return $"{this._remainingParticipants}/{this._participantCount}";
        }
    }
}