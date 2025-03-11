// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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