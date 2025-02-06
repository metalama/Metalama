// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System.ComponentModel;

#pragma warning disable SA1124

namespace Metalama.Patterns.Wpf;

/// <summary>
/// Exposes several methods that create instances of the <see cref="DelegateCommand"/> and <see cref="AsyncDelegateCommand"/> classes
/// for different signatures of the execution delegate, with or without parameter, with or without <see cref="CancellationToken"/>,
/// synchronous, asynchronous, and background.
/// </summary>
[PublicAPI]
public static class DelegateCommandFactory
{
    #region Synchronous

    public static DelegateCommand CreateDelegateCommand( Action execute, Func<bool>? canExecute ) => new( execute, canExecute );

    public static DelegateCommand CreateDelegateCommand(
        Action execute,
        Func<bool> canExecute,
        INotifyPropertyChanged canExecuteNotifier,
        string canExecutePropertyName )
        => new( execute, canExecute, canExecuteNotifier, canExecutePropertyName );

    public static DelegateCommand<T> CreateDelegateCommand<T>( Action<T> execute, Func<T, bool>? canExecute ) => new( execute, canExecute );

    public static DelegateCommand<T> CreateDelegateCommand<T>(
        Action<T> execute,
        Func<T, bool> canExecute,
        INotifyPropertyChanged canExecuteNotifier,
        string canExecutePropertyName )
        => new( execute, canExecute, canExecuteNotifier, canExecutePropertyName );

    #endregion

    #region Asynchronous

    public static AsyncDelegateCommand CreateAsyncDelegateCommand(
        Func<CancellationToken, Task> execute,
        Func<bool>? canExecute,
        bool supportsConcurrentExecution,
        bool runsInBackground )
        => new( RunInBackground( execute, runsInBackground ), canExecute, true, supportsConcurrentExecution );

    public static AsyncDelegateCommand CreateAsyncDelegateCommand(
        Func<Task> execute,
        Func<bool>? canExecute,
        bool supportsConcurrentExecution,
        bool runsInBackground )
        => new( RunInBackground( _ => execute(), runsInBackground ), canExecute, false, supportsConcurrentExecution );

    public static AsyncDelegateCommand CreateAsyncDelegateCommand(
        Func<CancellationToken, Task> execute,
        Func<bool> canExecute,
        INotifyPropertyChanged canExecuteNotifier,
        string canExecutePropertyName,
        bool supportsConcurrentExecution,
        bool runsInBackground )
        => new(
            RunInBackground( execute, runsInBackground ),
            canExecute,
            canExecuteNotifier,
            canExecutePropertyName,
            true,
            supportsConcurrentExecution );

    public static AsyncDelegateCommand<T> CreateAsyncDelegateCommand<T>(
        Func<T, CancellationToken, Task> execute,
        Func<T, bool> canExecute,
        INotifyPropertyChanged canExecuteNotifier,
        string canExecutePropertyName,
        bool supportsConcurrentExecution,
        bool runsInBackground )
        => new(
            RunInBackground( execute, runsInBackground ),
            canExecute,
            canExecuteNotifier,
            canExecutePropertyName,
            true,
            supportsConcurrentExecution );

    public static AsyncDelegateCommand<T> CreateAsyncDelegateCommand<T>(
        Func<T, CancellationToken, Task> execute,
        Func<T, bool> canExecute,
        bool supportsConcurrentExecution,
        bool runsInBackground )
        => new(
            RunInBackground( execute, runsInBackground ),
            canExecute,
            true,
            supportsConcurrentExecution );

    public static AsyncDelegateCommand<T> CreateAsyncDelegateCommand<T>(
        Func<T, Task> execute,
        Func<T, bool> canExecute,
        bool supportsConcurrentExecution,
        bool runsInBackground )
        => new(
            RunInBackground<T>( ( arg, _ ) => execute( arg ), runsInBackground ),
            canExecute,
            true,
            supportsConcurrentExecution );

    public static AsyncDelegateCommand CreateAsyncDelegateCommand(
        Func<Task> execute,
        Func<bool> canExecute,
        INotifyPropertyChanged canExecuteNotifier,
        string canExecutePropertyName,
        bool supportsConcurrentExecution,
        bool runsInBackground )
        => new(
            RunInBackground( _ => execute(), runsInBackground ),
            canExecute,
            canExecuteNotifier,
            canExecutePropertyName,
            true,
            supportsConcurrentExecution );

    public static AsyncDelegateCommand<T> CreateParametricAsyncDelegateCommand<T>(
        Func<T, CancellationToken, Task> execute,
        Func<T, bool>? canExecute,
        bool supportsCancellation,
        bool supportsConcurrentExecution,
        bool runsInBackground )
        => new( RunInBackground( execute, runsInBackground ), canExecute, supportsCancellation, supportsConcurrentExecution );

    public static AsyncDelegateCommand<T> CreateParametricAsyncDelegateCommand<T>(
        Func<T, Task> execute,
        Func<T, bool>? canExecute,
        bool supportsCancellation,
        bool supportsConcurrentExecution,
        bool runsInBackground )
        => new( RunInBackground<T>( ( arg, _ ) => execute( arg ), runsInBackground ), canExecute, supportsCancellation, supportsConcurrentExecution );

    #endregion

    #region Background

    public static AsyncDelegateCommand<T> CreateBackgroundDelegateCommand<T>(
        Action<T, CancellationToken> execute,
        Func<T, bool>? canExecute,
        bool supportsConcurrentExecution )
        => new( RunInBackground( execute ), canExecute, true, supportsConcurrentExecution );

    public static AsyncDelegateCommand<T> CreateBackgroundDelegateCommand<T>( Action<T> execute, Func<T, bool>? canExecute, bool supportsConcurrentExecution )
        => new( RunInBackground<T>( ( arg, _ ) => execute( arg ) ), canExecute, false, supportsConcurrentExecution );

    public static AsyncDelegateCommand CreateBackgroundDelegateCommand(
        Action<CancellationToken> execute,
        Func<bool>? canExecute,
        bool supportsConcurrentExecution )
        => new( RunInBackground( execute ), canExecute, true, supportsConcurrentExecution );

    public static AsyncDelegateCommand CreateBackgroundDelegateCommand( Action execute, Func<bool>? canExecute, bool supportsConcurrentExecution )
        => new( RunInBackground( _ => execute() ), canExecute, false, supportsConcurrentExecution );

    public static AsyncDelegateCommand CreateBackgroundDelegateCommand(
        Action<CancellationToken> execute,
        Func<bool> canExecute,
        INotifyPropertyChanged canExecuteNotifier,
        string canExecutePropertyName,
        bool supportsConcurrentExecution )
        => new( RunInBackground( execute ), canExecute, canExecuteNotifier, canExecutePropertyName, true, supportsConcurrentExecution );

    public static AsyncDelegateCommand<T> CreateBackgroundDelegateCommand<T>(
        Action<T, CancellationToken> execute,
        Func<T, bool> canExecute,
        INotifyPropertyChanged canExecuteNotifier,
        string canExecutePropertyName,
        bool supportsConcurrentExecution )
        => new( RunInBackground( execute ), canExecute, canExecuteNotifier, canExecutePropertyName, true, supportsConcurrentExecution );

    public static AsyncDelegateCommand CreateBackgroundDelegateCommand(
        Action execute,
        Func<bool> canExecute,
        INotifyPropertyChanged canExecuteNotifier,
        string canExecutePropertyName,
        bool supportsConcurrentExecution )
        => new( RunInBackground( _ => execute() ), canExecute, canExecuteNotifier, canExecutePropertyName, false, supportsConcurrentExecution );

    public static AsyncDelegateCommand<T> CreateBackgroundDelegateCommand<T>(
        Action<T> execute,
        Func<T, bool> canExecute,
        INotifyPropertyChanged canExecuteNotifier,
        string canExecutePropertyName,
        bool supportsConcurrentExecution )
        => new(
            RunInBackground<T>( ( arg, _ ) => execute( arg ) ),
            canExecute,
            canExecuteNotifier,
            canExecutePropertyName,
            false,
            supportsConcurrentExecution );

    #endregion

    private static Func<CancellationToken, Task> RunInBackground( Action<CancellationToken> execute ) => ct => Task.Run( () => execute( ct ), ct );

    private static Func<T, CancellationToken, Task> RunInBackground<T>( Action<T, CancellationToken> execute )
        => ( arg, ct ) => Task.Run( () => execute( arg, ct ), ct );

    private static Func<CancellationToken, Task> RunInBackground( Func<CancellationToken, Task> execute, bool runsInBackground )
        => runsInBackground ? ct => Task.Run( () => execute( ct ), ct ) : execute;

    private static Func<T, CancellationToken, Task> RunInBackground<T>( Func<T, CancellationToken, Task> execute, bool runsInBackground )
        => runsInBackground ? ( arg, ct ) => Task.Run( () => execute( arg, ct ), ct ) : execute;
}