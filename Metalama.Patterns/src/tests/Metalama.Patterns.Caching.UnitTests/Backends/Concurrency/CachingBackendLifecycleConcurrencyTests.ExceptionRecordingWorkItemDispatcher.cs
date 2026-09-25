// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency
{
    public sealed partial class CachingBackendLifecycleConcurrencyTests
    {
        /// <summary>
        /// An <see cref="IWorkItemDispatcher"/> that runs each work item on a new <see cref="ConcurrencyTestWorker"/>,
        /// which records the exception that escapes the work item.
        /// </summary>
        /// <remarks>
        /// <para>
        /// An exception that escapes a work item of <see cref="ThreadPoolWorkItemDispatcher"/> terminates the process,
        /// and the <c>TestWorkItemDispatcher</c> class of the test helpers lets it escape in the same way. This dispatcher
        /// records the exception instead, so that a test can assert that no exception escaped without terminating the
        /// test host.
        /// </para>
        /// <para>
        /// A new thread always receives the execution context of the thread that starts it, so the dispatcher ignores
        /// the <c>flowExecutionContext</c> parameter of <see cref="Dispatch"/>. The backend requests that the execution
        /// context flows when it raises an event.
        /// </para>
        /// </remarks>
        private sealed class ExceptionRecordingWorkItemDispatcher : IWorkItemDispatcher
        {
            /// <summary>
            /// The object that protects <see cref="_workers"/>.
            /// </summary>
            private readonly object _sync = new();

            /// <summary>
            /// The workers that run the dispatched work items, in the order of dispatch.
            /// </summary>
            private readonly List<ConcurrencyTestWorker> _workers = new();

            /// <summary>
            /// Starts a worker that runs the work item.
            /// </summary>
            /// <param name="workItem">The delegate to execute.</param>
            /// <param name="state">The object passed to <paramref name="workItem"/>.</param>
            /// <param name="flowExecutionContext">Ignored. The execution context always flows to the worker.</param>
            public void Dispatch( WaitCallback workItem, object? state, bool flowExecutionContext = true )
            {
                lock ( this._sync )
                {
                    var worker = ConcurrencyTestWorker.Start( $"WorkItem{this._workers.Count + 1}", () => workItem( state ) );

                    this._workers.Add( worker );
                }
            }

            /// <summary>
            /// Gets the workers of the work items dispatched so far, in the order of dispatch.
            /// </summary>
            /// <returns>A copy of the list of workers.</returns>
            public IReadOnlyList<ConcurrencyTestWorker> GetWorkers()
            {
                lock ( this._sync )
                {
                    return this._workers.ToArray();
                }
            }
        }
    }
}
