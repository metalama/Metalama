// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System.ComponentModel;

namespace Metalama.Patterns.Wpf;

/// <summary>
/// An implementation of <see cref="IAsyncCommand"/> which uses delegates to access callbacks, accepting no parameters.
/// </summary>
[PublicAPI]
public sealed class AsyncDelegateCommand : BaseAsyncDelegateCommand
{
    private readonly Func<bool>? _canExecute;
    private readonly Func<CancellationToken, Task> _execute;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDelegateCommand"/> class, without <see cref="INotifyPropertyChanged"/> integration.
    /// </summary>
    internal AsyncDelegateCommand(
        Func<CancellationToken, Task> execute,
        Func<bool>? canExecute,
        bool supportsCancellation,
        bool supportsConcurrentExecution ) : base( supportsCancellation, supportsConcurrentExecution )
    {
        this._execute = execute;
        this._canExecute = canExecute;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDelegateCommand"/> class, with <see cref="INotifyPropertyChanged"/> integration.
    /// </summary>
    internal AsyncDelegateCommand(
        Func<CancellationToken, Task> execute,
        Func<bool> canExecute,
        INotifyPropertyChanged canExecutePropertyChangeNotifier,
        string canExecutePropertyName,
        bool supportsCancellation,
        bool supportsConcurrentExecution ) : base( canExecutePropertyChangeNotifier, canExecutePropertyName, supportsCancellation, supportsConcurrentExecution )
    {
        this._execute = execute;
        this._canExecute = canExecute;
    }

    /// <summary>
    /// Gets a value indicating whether the <see cref="Execute"/> can be invoked.
    /// </summary>
    public bool CanExecute => this.OuterCanExecute( null );

    /// <summary>
    /// Executed the command.
    /// </summary>
    public DelegateCommandExecution Execute() => this.OuterExecute( null );

    private protected override bool InnerCanExecute( object? parameter ) => this._canExecute == null || this._canExecute.Invoke();

    private protected override Task InnerExecuteAsync( object? parameter, CancellationToken cancellationToken ) => this._execute( cancellationToken );
}