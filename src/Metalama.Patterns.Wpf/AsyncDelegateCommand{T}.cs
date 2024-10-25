// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System.ComponentModel;

namespace Metalama.Patterns.Wpf;

/// <summary>
/// An implementation of <see cref="IAsyncCommand"/> which uses delegates to access callbacks, accepting one parameter.
/// </summary>
[PublicAPI]
public sealed class AsyncDelegateCommand<T> : BaseAsyncDelegateCommand
{
    private readonly Func<T, bool>? _canExecute;
    private readonly Func<T, CancellationToken, Task> _execute;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDelegateCommand{T}"/> class, without <see cref="INotifyPropertyChanged"/> integration.
    /// </summary>
    internal AsyncDelegateCommand(
        Func<T, CancellationToken, Task> execute,
        Func<T, bool>? canExecute,
        bool supportsCancellation,
        bool supportsConcurrentExecution ) : base( supportsCancellation, supportsConcurrentExecution )
    {
        this._execute = execute;
        this._canExecute = canExecute;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDelegateCommand{T}"/> class, with <see cref="INotifyPropertyChanged"/> integration.
    /// </summary>
    internal AsyncDelegateCommand(
        Func<T, CancellationToken, Task> execute,
        Func<T, bool> canExecute,
        INotifyPropertyChanged canExecutePropertyChangeNotifier,
        string canExecutePropertyName,
        bool supportsCancellation,
        bool supportsConcurrentExecution ) : base( canExecutePropertyChangeNotifier, canExecutePropertyName, supportsCancellation, supportsConcurrentExecution )
    {
        this._execute = execute;
        this._canExecute = canExecute;
    }
    
    /// <summary>
    /// Gets a value indicating whether the <see cref="Execute"/> command can be called with a given parameter.
    /// </summary>
    public bool CanExecute( T parameter ) => this.OuterCanExecute( parameter );

    /// <summary>
    /// Executes the command with a given parameter.
    /// </summary>
    /// <param name="parameter"></param>
    public DelegateCommandExecution Execute( T parameter ) => this.OuterExecute( parameter );

    private protected override bool InnerCanExecute( object? parameter ) => this._canExecute == null || this._canExecute.Invoke( (T) parameter! );

    private protected override Task InnerExecuteAsync( object? parameter, CancellationToken cancellationToken )
        => this._execute( (T) parameter!, cancellationToken );
}