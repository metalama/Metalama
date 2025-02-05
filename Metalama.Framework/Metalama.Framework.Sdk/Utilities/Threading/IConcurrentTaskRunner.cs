// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Utilities.Threading;

// Must be project-scoped because the option to enable/disable concurrency is in the project options.
public interface IConcurrentTaskRunner : IProjectService
{
    Task RunConcurrentlyAsync<T>( IEnumerable<T> items, Action<T> action, CancellationToken cancellationToken )
        where T : notnull;

    // ReSharper disable once UnusedMember.Global
    Task RunConcurrentlyAsync<TItem, TContext>(
        IEnumerable<TItem> items,
        Action<TItem, TContext> action,
        Func<TContext> createContext,
        CancellationToken cancellationToken )
        where TItem : notnull
        where TContext : IDisposable;

    Task RunConcurrentlyAsync<T>( IEnumerable<T> items, Func<T, Task> action, CancellationToken cancellationToken )
        where T : notnull;
}