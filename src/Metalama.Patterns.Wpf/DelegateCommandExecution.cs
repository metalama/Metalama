// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf;

/// <summary>
/// Represents a distinct execution of an <see cref="AsyncDelegateCommand"/>, exposing its <see cref="Task"/>,
/// and allowing to cancel it.
/// </summary>
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