// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System.ComponentModel;
using System.Windows.Input;

namespace Metalama.Patterns.Wpf;

/// <summary>
/// Represents an <see cref="ICommand"/> that is executed asynchronously.
/// </summary>
[PublicAPI]
public interface IAsyncCommand : ICommand, INotifyPropertyChanged
{
    /// <summary>
    /// Cancels all pending executions of the current command.
    /// </summary>
    void Cancel();

    /// <summary>
    /// Gets a value indicating whether the <see cref="Cancel"/> command can be called.
    /// </summary>
    bool CanCancel { get; }

    /// <summary>
    /// Gets a value indicating whether the <see cref="Cancel"/> has been called for any pending command execution.
    /// </summary>
    bool IsCancellationRequested { get; }

    /// <summary>
    /// Gets a value indicating whether any command is currently executing.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the <see cref="Task"/> of the last execution.
    /// </summary>
    Task? ExecutionTask { get; }
}