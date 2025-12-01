// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.ComponentModel;
using System.Windows.Input;

namespace Metalama.Patterns.Wpf;

/// <summary>
/// Represents an <see cref="ICommand"/> that executes asynchronously, providing properties to monitor execution state
/// and methods to cancel running executions.
/// </summary>
/// <remarks>
/// <para>This interface extends both <see cref="ICommand"/> and <see cref="INotifyPropertyChanged"/> to enable
/// data binding to command state properties in WPF applications.</para>
/// </remarks>
/// <seealso cref="AsyncDelegateCommand"/>
/// <seealso cref="AsyncDelegateCommand{T}"/>
/// <seealso cref="DelegateCommandExecution"/>
/// <seealso href="@wpf-command"/>
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