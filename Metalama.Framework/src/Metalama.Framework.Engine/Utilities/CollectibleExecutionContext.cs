// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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