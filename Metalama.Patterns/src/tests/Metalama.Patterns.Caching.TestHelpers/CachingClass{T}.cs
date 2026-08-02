// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.TestHelpers
{
    public partial class CachingClass<T>
        where T : CachedValueClass, new()
    {
        private int _counter;

        /// <summary>
        /// A value indicating whether the cached method has been called since the last call to <see cref="Reset"/>,
        /// where <c>0</c> means <c>false</c> and <c>1</c> means <c>true</c>.
        /// </summary>
        /// <remarks>
        /// The asynchronous methods set this field on the thread that resumes their body, while tests read it on their
        /// own thread, therefore all accesses go through <see cref="Interlocked"/> and the field is an <see cref="int"/>
        /// rather than a <see cref="bool"/>.
        /// </remarks>
        private int _methodCalled;

        /// <summary>
        /// The signal on which the asynchronous methods wait before executing their body, or <c>null</c> if they are
        /// not suspended. See <see cref="SuspendAsyncMethods"/>.
        /// </summary>
        private TaskCompletionSource<bool>? _suspension;

        // ReSharper disable once EventNeverSubscribedTo.Global
        public event EventHandler<T>? MethodCalled;

        /// <summary>
        /// Suspends the asynchronous methods of this object immediately before they execute their body, until
        /// <see cref="ResumeAsyncMethods"/> is called.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method allows a test to observe the object at a point where an asynchronous method has been started but
        /// its body has not run yet, which is otherwise not observable in a deterministic manner: without a suspension
        /// point that the test itself controls, the test would depend on the body being slower than the statements that
        /// follow the call, and that assumption does not hold on a loaded machine.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">The asynchronous methods are already suspended.</exception>
        public void SuspendAsyncMethods()
        {
            var suspension = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

            // The test and the suspended method bodies run on different threads, therefore the transition from the
            // resumed state to the suspended state must be atomic: a check followed by a separate write would let two
            // concurrent calls both succeed, and one of the two signals would then have no matching resumption.
            if ( Interlocked.CompareExchange( ref this._suspension, suspension, null ) != null )
            {
                throw new InvalidOperationException( "The asynchronous methods are already suspended." );
            }
        }

        /// <summary>
        /// Resumes the asynchronous methods suspended by <see cref="SuspendAsyncMethods"/>, and lets the asynchronous
        /// methods started afterwards execute their body without suspension.
        /// </summary>
        /// <exception cref="InvalidOperationException">The asynchronous methods are not suspended.</exception>
        public void ResumeAsyncMethods()
        {
            var suspension = Interlocked.Exchange( ref this._suspension, null );

            if ( suspension == null )
            {
                throw new InvalidOperationException( "The asynchronous methods are not suspended." );
            }

            suspension.SetResult( true );
        }

        public bool Reset()
        {
            return Interlocked.Exchange( ref this._methodCalled, 0 ) != 0;
        }

        private T CreateNextValue()
        {
            if ( Interlocked.Exchange( ref this._methodCalled, 1 ) != 0 )
            {
                throw new InvalidOperationException(
                    "Cached method called twice unexpectedly. If this is the expected behavior, call reset before the second call of the method." );
            }

            var value = new T() { Id = this._counter++ };
            this.MethodCalled?.Invoke( this, value );

            return value;
        }

        private async Task<T> CreateNextValueAsync()
        {
            var suspension = Volatile.Read( ref this._suspension );

            if ( suspension == null )
            {
                // Complete asynchronously, so that the caller observes a genuinely asynchronous method. A timer is
                // deliberately not used here: the point at which the body runs must never be defined by a duration,
                // because a test that asserts the ordering would then be racing that duration.
                await Task.Yield();
            }
            else
            {
                await suspension.Task.WaitWithTimeoutAsync( "The test did not resume the asynchronous methods." );
            }

            return this.CreateNextValue();
        }

        private T CreateNextValueAsDependency()
        {
            var value = this.CreateNextValue();
            CachingService.Default.AddObjectDependency( value );

            return value;
        }

        private async Task<T> CreateNextValueAsDependencyAsync()
        {
            var value = await this.CreateNextValueAsync();
            CachingService.Default.AddObjectDependency( value );

            return value;
        }

        public virtual T GetValue()
        {
            return this.CreateNextValue();
        }

        public virtual async Task<T> GetValueAsync()
        {
            return await this.CreateNextValueAsync();
        }

        public virtual T GetValueAsDependency()
        {
            return this.CreateNextValueAsDependency();
        }

        public virtual async Task<T> GetValueAsDependencyAsync()
        {
            return await this.CreateNextValueAsDependencyAsync();
        }

        // ReSharper disable once VirtualMemberNeverOverridden.Global
        // ReSharper disable once UnusedMember.Global
        public virtual IEnumerable<T> GetValues()
        {
            yield return this.CreateNextValue();
        }
    }
}