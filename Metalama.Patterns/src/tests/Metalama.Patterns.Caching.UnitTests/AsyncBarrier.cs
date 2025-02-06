// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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