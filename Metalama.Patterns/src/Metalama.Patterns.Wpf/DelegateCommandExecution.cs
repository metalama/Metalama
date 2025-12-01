// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Wpf;

/// <summary>
/// Represents a single execution of an asynchronous delegate command, providing access to the execution's <see cref="Task"/>
/// and allowing cancellation of that specific invocation.
/// </summary>
/// <remarks>
/// <para>This struct is returned by the <see cref="AsyncDelegateCommand.Execute()"/> and <see cref="AsyncDelegateCommand{T}.Execute(T)"/> methods,
/// and is also provided through the <see cref="BaseAsyncDelegateCommand.Executed"/> event.</para>
/// <para>Use this type to track and cancel individual command executions, especially when concurrent execution is enabled
/// via <see cref="CommandAttribute.SupportsConcurrentExecution"/>.</para>
/// </remarks>
/// <seealso cref="AsyncDelegateCommand"/>
/// <seealso cref="AsyncDelegateCommand{T}"/>
/// <seealso cref="BaseAsyncDelegateCommand"/>
/// <seealso href="@wpf-command"/>
[PublicAPI]
public readonly struct DelegateCommandExecution
{
    private readonly CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Gets the <see cref="System.Threading.Tasks.Task"/> representing this execution of the command.
    /// </summary>
    public Task Task { get; }

    /// <summary>
    /// Cancels this specific execution of the command by signaling the <see cref="CancellationToken"/>
    /// that was passed to the execute delegate.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the command does not support cancellation.</exception>
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

    /// <summary>
    /// Gets a value indicating whether <see cref="Cancel"/> has been called for this execution.
    /// </summary>
    public bool IsCancellationRequested => this._cancellationTokenSource?.IsCancellationRequested ?? false;

    internal DelegateCommandExecution( CancellationTokenSource? cancellationTokenSource, Task task )
    {
        this._cancellationTokenSource = cancellationTokenSource;
        this.Task = task;
    }
}