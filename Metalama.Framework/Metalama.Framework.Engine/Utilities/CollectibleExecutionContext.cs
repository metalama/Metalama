// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Metalama.Framework.Engine.Utilities
{
    /// <summary>
    /// Stores async-local data about the current test.
    /// </summary>
    public sealed class CollectibleExecutionContext : IDisposable
    {
        private static readonly AsyncLocal<CollectibleExecutionContext> _current = new();

        private readonly ConcurrentQueue<Action> _disposeActions = new();

        private CollectibleExecutionContext() { }

        // Resharper disable UnusedMember.Global
        public static void RegisterDisposeAction( Action action )
        {
            _current.Value?._disposeActions.Enqueue( action );
        }

        public static CollectibleExecutionContext Open()
        {
            CollectibleExecutionContext executionContext = new();
            _current.Value = executionContext;

            return executionContext;
        }

        public void Dispose()
        {
            foreach ( var action in this._disposeActions )
            {
                action();
            }
        }
    }
}