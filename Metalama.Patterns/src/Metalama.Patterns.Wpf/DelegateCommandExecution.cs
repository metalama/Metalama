// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Wpf;

/// <summary>
/// Represents a distinct execution of an <see cref="AsyncDelegateCommand"/>, exposing its <see cref="Task"/>,
/// and allowing to cancel it.
/// </summary>
[PublicAPI]
public readonly struct DelegateCommandExecution
{
    private readonly CancellationTokenSource? _cancellationTokenSource;

    public Task Task { get; }

    public void Cancel()
    {
        if ( this._cancellationTokenSource != null )
        {
            this._cancellationTokenSource.Cancel();
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    public bool IsCancellationRequested => this._cancellationTokenSource?.IsCancellationRequested ?? false;

    internal DelegateCommandExecution( CancellationTokenSource? cancellationTokenSource, Task task )
    {
        this._cancellationTokenSource = cancellationTokenSource;
        this.Task = task;
    }
}