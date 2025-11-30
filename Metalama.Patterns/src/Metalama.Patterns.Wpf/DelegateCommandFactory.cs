// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.ComponentModel;

#pragma warning disable SA1124

namespace Metalama.Patterns.Wpf;

/// <summary>
/// Factory class that creates instances of <see cref="DelegateCommand"/>, <see cref="DelegateCommand{T}"/>,
/// <see cref="AsyncDelegateCommand"/>, and <see cref="AsyncDelegateCommand{T}"/>. This class is used internally
/// by the <see cref="CommandAttribute"/> aspect to generate command properties.
/// </summary>
/// <remarks>
/// <para>This factory provides methods for creating commands with various configurations:</para>
/// <para>- <b>Synchronous commands</b>: Created via <c>CreateDelegateCommand</c> methods.</para>
/// <para>- <b>Asynchronous commands</b>: Created via <c>CreateAsyncDelegateCommand</c> methods for <see cref="Task"/>-returning methods.</para>
/// <para>- <b>Background commands</b>: Created via <c>CreateBackgroundDelegateCommand</c> methods, which dispatch execution to a background thread.</para>
/// <para>Each method has overloads for commands with or without parameters, with or without <see cref="INotifyPropertyChanged"/> integration.</para>
/// </remarks>
/// <seealso cref="CommandAttribute"/>
/// <seealso cref="DelegateCommand"/>
/// <seealso cref="AsyncDelegateCommand"/>
/// <seealso href="@wpf-command"/>
[PublicAPI]
public static class DelegateCommandFactory
{
    #region Synchronous

    /// <summary>
    /// Creates a synchronous command without a parameter.
    /// </summary>
    /// <param name="execute">The delegate to invoke when the command is executed.</param>
    /// <param name="canExecute">An optional delegate that determines whether the command can execute. If <c>null</c>, the command can always execute.</param>
    /// <returns>A new <see cref="DelegateCommand"/> instance.</returns>
    public static DelegateCommand CreateDelegateCommand( Action execute, Func<bool>? canExecute ) => new( execute, canExecute );

    /// <summary>
    /// Creates a synchronous command without a parameter, with <see cref="INotifyPropertyChanged"/> integration for the <c>CanExecute</c> property.
    /// </summary>
    /// <param name="execute">The delegate to invoke when the command is executed.</param>
    /// <param name="canExecute">A delegate that determines whether the command can execute.</param>
    /// <param name="canExecuteNotifier">The object that raises <see cref="INotifyPropertyChanged.PropertyChanged"/> when the <c>CanExecute</c> property changes.</param>
    /// <param name="canExecutePropertyName">The name of the property that controls <c>CanExecute</c>.</param>
    /// <returns>A new <see cref="DelegateCommand"/> instance that automatically raises <see cref="System.Windows.Input.ICommand.CanExecuteChanged"/> when the specified property changes.</returns>
    public static DelegateCommand CreateDelegateCommand(
        Action execute,
        Func<bool> canExecute,
        INotifyPropertyChanged canExecuteNotifier,
        string canExecutePropertyName )
        => new( execute, canExecute, canExecuteNotifier, canExecutePropertyName );

    /// <summary>
    /// Creates a synchronous command that accepts a parameter.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <param name="execute">The delegate to invoke when the command is executed.</param>
    /// <param name="canExecute">An optional delegate that determines whether the command can execute for a given parameter. If <c>null</c>, the command can always execute.</param>
    /// <returns>A new <see cref="DelegateCommand{T}"/> instance.</returns>
    public static DelegateCommand<T> CreateDelegateCommand<T>( Action<T> execute, Func<T, bool>? canExecute ) => new( execute, canExecute );

    /// <summary>
    /// Creates a synchronous command that accepts a parameter, with <see cref="INotifyPropertyChanged"/> integration for the <c>CanExecute</c> property.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <param name="execute">The delegate to invoke when the command is executed.</param>
    /// <param name="canExecute">A delegate that determines whether the command can execute for a given parameter.</param>
    /// <param name="canExecuteNotifier">The object that raises <see cref="INotifyPropertyChanged.PropertyChanged"/> when the <c>CanExecute</c> property changes.</param>
    /// <param name="canExecutePropertyName">The name of the property that controls <c>CanExecute</c>.</param>
    /// <returns>A new <see cref="DelegateCommand{T}"/> instance that automatically raises <see cref="System.Windows.Input.ICommand.CanExecuteChanged"/> when the specified property changes.</returns>
    public static DelegateCommand<T> CreateDelegateCommand<T>(
        Action<T> execute,
        Func<T, bool> canExecute,
        INotifyPropertyChanged canExecuteNotifier,
        string canExecutePropertyName )
        => new( execute, canExecute, canExecuteNotifier, canExecutePropertyName );

    #endregion

    #region Asynchronous

    /// <summary>
    /// Creates an asynchronous command without a parameter, with cancellation support.
    /// </summary>
    /// <param name="execute">The async delegate to invoke when the command is executed. Receives a <see cref="CancellationToken"/> for cancellation support.</param>
    /// <param name="canExecute">An optional delegate that determines whether the command can execute. If <c>null</c>, the command can always execute.</param>
    /// <param name="supportsConcurrentExecution">If <c>true</c>, allows multiple concurrent executions of the command.</param>
    /// <param name="runsInBackground">If <c>true</c>, the execution is dispatched to a background thread via <see cref="Task.Run(Func{Task}, CancellationToken)"/>.</param>
    /// <returns>A new <see cref="AsyncDelegateCommand"/> instance.</returns>
    public static AsyncDelegateCommand CreateAsyncDelegateCommand(
        Func<CancellationToken, Task> execute,
        Func<bool>? canExecute,
        bool supportsConcurrentExecution,
        bool runsInBackground )
        => new( RunInBackground( execute, runsInBackground ), canExecute, true, supportsConcurrentExecution );

    /// <summary>
    /// Creates an asynchronous command without a parameter, without cancellation support.
    /// </summary>
    /// <param name="execute">The async delegate to invoke when the command is executed.</param>
    /// <param name="canExecute">An optional delegate that determines whether the command can execute. If <c>null</c>, the command can always execute.</param>
    /// <param name="supportsConcurrentExecution">If <c>true</c>, allows multiple concurrent executions of the command.</param>
    /// <param name="runsInBackground">If <c>true</c>, the execution is dispatched to a background thread via <see cref="Task.Run(Func{Task}, CancellationToken)"/>.</param>
    /// <returns>A new <see cref="AsyncDelegateCommand"/> instance.</returns>
    public static AsyncDelegateCommand CreateAsyncDelegateCommand(
        Func<Task> execute,
        Func<bool>? canExecute,
        bool supportsConcurrentExecution,
        bool runsInBackground )
        => new( RunInBackground( _ => execute(), runsInBackground ), canExecute, false, supportsConcurrentExecution );

    /// <summary>
    /// Creates an asynchronous command without a parameter, with cancellation support and <see cref="INotifyPropertyChanged"/> integration.
    /// </summary>
    /// <param name="execute">The async delegate to invoke when the command is executed. Receives a <see cref="CancellationToken"/> for cancellation support.</param>
    /// <param name="canExecute">A delegate that determines whether the command can execute.</param>
    /// <param name="canExecuteNotifier">The object that raises <see cref="INotifyPropertyChanged.PropertyChanged"/> when the <c>CanExecute</c> property changes.</param>
    /// <param name="canExecutePropertyName">The name of the property that controls <c>CanExecute</c>.</param>
    /// <param name="supportsConcurrentExecution">If <c>true</c>, allows multiple concurrent executions of the command.</param>
    /// <param name="runsInBackground">If <c>true</c>, the execution is dispatched to a background thread via <see cref="Task.Run(Func{Task}, CancellationToken)"/>.</param>
    /// <returns>A new <see cref="AsyncDelegateCommand"/> instance that automatically raises <see cref="System.Windows.Input.ICommand.CanExecuteChanged"/> when the specified property changes.</returns>
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

    /// <summary>
    /// Creates an asynchronous command that accepts a parameter, with cancellation support and <see cref="INotifyPropertyChanged"/> integration.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <param name="execute">The async delegate to invoke when the command is executed. Receives the parameter and a <see cref="CancellationToken"/>.</param>
    /// <param name="canExecute">A delegate that determines whether the command can execute for a given parameter.</param>
    /// <param name="canExecuteNotifier">The object that raises <see cref="INotifyPropertyChanged.PropertyChanged"/> when the <c>CanExecute</c> property changes.</param>
    /// <param name="canExecutePropertyName">The name of the property that controls <c>CanExecute</c>.</param>
    /// <param name="supportsConcurrentExecution">If <c>true</c>, allows multiple concurrent executions of the command.</param>
    /// <param name="runsInBackground">If <c>true</c>, the execution is dispatched to a background thread via <see cref="Task.Run(Func{Task}, CancellationToken)"/>.</param>
    /// <returns>A new <see cref="AsyncDelegateCommand{T}"/> instance that automatically raises <see cref="System.Windows.Input.ICommand.CanExecuteChanged"/> when the specified property changes.</returns>
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

    /// <summary>
    /// Creates an asynchronous command that accepts a parameter, with cancellation support.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <param name="execute">The async delegate to invoke when the command is executed. Receives the parameter and a <see cref="CancellationToken"/>.</param>
    /// <param name="canExecute">A delegate that determines whether the command can execute for a given parameter.</param>
    /// <param name="supportsConcurrentExecution">If <c>true</c>, allows multiple concurrent executions of the command.</param>
    /// <param name="runsInBackground">If <c>true</c>, the execution is dispatched to a background thread via <see cref="Task.Run(Func{Task}, CancellationToken)"/>.</param>
    /// <returns>A new <see cref="AsyncDelegateCommand{T}"/> instance.</returns>
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

    /// <summary>
    /// Creates an asynchronous command that accepts a parameter, without cancellation support.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <param name="execute">The async delegate to invoke when the command is executed.</param>
    /// <param name="canExecute">A delegate that determines whether the command can execute for a given parameter.</param>
    /// <param name="supportsConcurrentExecution">If <c>true</c>, allows multiple concurrent executions of the command.</param>
    /// <param name="runsInBackground">If <c>true</c>, the execution is dispatched to a background thread via <see cref="Task.Run(Func{Task}, CancellationToken)"/>.</param>
    /// <returns>A new <see cref="AsyncDelegateCommand{T}"/> instance.</returns>
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

    /// <summary>
    /// Creates an asynchronous command without a parameter, without cancellation support, with <see cref="INotifyPropertyChanged"/> integration.
    /// </summary>
    /// <param name="execute">The async delegate to invoke when the command is executed.</param>
    /// <param name="canExecute">A delegate that determines whether the command can execute.</param>
    /// <param name="canExecuteNotifier">The object that raises <see cref="INotifyPropertyChanged.PropertyChanged"/> when the <c>CanExecute</c> property changes.</param>
    /// <param name="canExecutePropertyName">The name of the property that controls <c>CanExecute</c>.</param>
    /// <param name="supportsConcurrentExecution">If <c>true</c>, allows multiple concurrent executions of the command.</param>
    /// <param name="runsInBackground">If <c>true</c>, the execution is dispatched to a background thread via <see cref="Task.Run(Func{Task}, CancellationToken)"/>.</param>
    /// <returns>A new <see cref="AsyncDelegateCommand"/> instance that automatically raises <see cref="System.Windows.Input.ICommand.CanExecuteChanged"/> when the specified property changes.</returns>
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

    /// <summary>
    /// Creates an asynchronous command that accepts a parameter, with configurable cancellation support.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <param name="execute">The async delegate to invoke when the command is executed. Receives the parameter and a <see cref="CancellationToken"/>.</param>
    /// <param name="canExecute">An optional delegate that determines whether the command can execute for a given parameter. If <c>null</c>, the command can always execute.</param>
    /// <param name="supportsCancellation">If <c>true</c>, enables cancellation support via <see cref="CancellationToken"/>.</param>
    /// <param name="supportsConcurrentExecution">If <c>true</c>, allows multiple concurrent executions of the command.</param>
    /// <param name="runsInBackground">If <c>true</c>, the execution is dispatched to a background thread via <see cref="Task.Run(Func{Task}, CancellationToken)"/>.</param>
    /// <returns>A new <see cref="AsyncDelegateCommand{T}"/> instance.</returns>
    public static AsyncDelegateCommand<T> CreateParametricAsyncDelegateCommand<T>(
        Func<T, CancellationToken, Task> execute,
        Func<T, bool>? canExecute,
        bool supportsCancellation,
        bool supportsConcurrentExecution,
        bool runsInBackground )
        => new( RunInBackground( execute, runsInBackground ), canExecute, supportsCancellation, supportsConcurrentExecution );

    /// <summary>
    /// Creates an asynchronous command that accepts a parameter, without cancellation token in the delegate, with configurable cancellation support.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <param name="execute">The async delegate to invoke when the command is executed.</param>
    /// <param name="canExecute">An optional delegate that determines whether the command can execute for a given parameter. If <c>null</c>, the command can always execute.</param>
    /// <param name="supportsCancellation">If <c>true</c>, enables cancellation support (though the delegate won't receive the token).</param>
    /// <param name="supportsConcurrentExecution">If <c>true</c>, allows multiple concurrent executions of the command.</param>
    /// <param name="runsInBackground">If <c>true</c>, the execution is dispatched to a background thread via <see cref="Task.Run(Func{Task}, CancellationToken)"/>.</param>
    /// <returns>A new <see cref="AsyncDelegateCommand{T}"/> instance.</returns>
    public static AsyncDelegateCommand<T> CreateParametricAsyncDelegateCommand<T>(
        Func<T, Task> execute,
        Func<T, bool>? canExecute,
        bool supportsCancellation,
        bool supportsConcurrentExecution,
        bool runsInBackground )
        => new( RunInBackground<T>( ( arg, _ ) => execute( arg ), runsInBackground ), canExecute, supportsCancellation, supportsConcurrentExecution );

    #endregion

    #region Background

    /// <summary>
    /// Creates a background command that accepts a parameter, with cancellation support. The execution is always dispatched to a background thread.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <param name="execute">The synchronous delegate to invoke on a background thread. Receives the parameter and a <see cref="CancellationToken"/>.</param>
    /// <param name="canExecute">An optional delegate that determines whether the command can execute for a given parameter. If <c>null</c>, the command can always execute.</param>
    /// <param name="supportsConcurrentExecution">If <c>true</c>, allows multiple concurrent executions of the command.</param>
    /// <returns>A new <see cref="AsyncDelegateCommand{T}"/> instance that executes on a background thread.</returns>
    public static AsyncDelegateCommand<T> CreateBackgroundDelegateCommand<T>(
        Action<T, CancellationToken> execute,
        Func<T, bool>? canExecute,
        bool supportsConcurrentExecution )
        => new( RunInBackground( execute ), canExecute, true, supportsConcurrentExecution );

    /// <summary>
    /// Creates a background command that accepts a parameter, without cancellation support. The execution is always dispatched to a background thread.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <param name="execute">The synchronous delegate to invoke on a background thread.</param>
    /// <param name="canExecute">An optional delegate that determines whether the command can execute for a given parameter. If <c>null</c>, the command can always execute.</param>
    /// <param name="supportsConcurrentExecution">If <c>true</c>, allows multiple concurrent executions of the command.</param>
    /// <returns>A new <see cref="AsyncDelegateCommand{T}"/> instance that executes on a background thread.</returns>
    public static AsyncDelegateCommand<T> CreateBackgroundDelegateCommand<T>( Action<T> execute, Func<T, bool>? canExecute, bool supportsConcurrentExecution )
        => new( RunInBackground<T>( ( arg, _ ) => execute( arg ) ), canExecute, false, supportsConcurrentExecution );

    /// <summary>
    /// Creates a background command without a parameter, with cancellation support. The execution is always dispatched to a background thread.
    /// </summary>
    /// <param name="execute">The synchronous delegate to invoke on a background thread. Receives a <see cref="CancellationToken"/>.</param>
    /// <param name="canExecute">An optional delegate that determines whether the command can execute. If <c>null</c>, the command can always execute.</param>
    /// <param name="supportsConcurrentExecution">If <c>true</c>, allows multiple concurrent executions of the command.</param>
    /// <returns>A new <see cref="AsyncDelegateCommand"/> instance that executes on a background thread.</returns>
    public static AsyncDelegateCommand CreateBackgroundDelegateCommand(
        Action<CancellationToken> execute,
        Func<bool>? canExecute,
        bool supportsConcurrentExecution )
        => new( RunInBackground( execute ), canExecute, true, supportsConcurrentExecution );

    /// <summary>
    /// Creates a background command without a parameter, without cancellation support. The execution is always dispatched to a background thread.
    /// </summary>
    /// <param name="execute">The synchronous delegate to invoke on a background thread.</param>
    /// <param name="canExecute">An optional delegate that determines whether the command can execute. If <c>null</c>, the command can always execute.</param>
    /// <param name="supportsConcurrentExecution">If <c>true</c>, allows multiple concurrent executions of the command.</param>
    /// <returns>A new <see cref="AsyncDelegateCommand"/> instance that executes on a background thread.</returns>
    public static AsyncDelegateCommand CreateBackgroundDelegateCommand( Action execute, Func<bool>? canExecute, bool supportsConcurrentExecution )
        => new( RunInBackground( _ => execute() ), canExecute, false, supportsConcurrentExecution );

    /// <summary>
    /// Creates a background command without a parameter, with cancellation support and <see cref="INotifyPropertyChanged"/> integration. The execution is always dispatched to a background thread.
    /// </summary>
    /// <param name="execute">The synchronous delegate to invoke on a background thread. Receives a <see cref="CancellationToken"/>.</param>
    /// <param name="canExecute">A delegate that determines whether the command can execute.</param>
    /// <param name="canExecuteNotifier">The object that raises <see cref="INotifyPropertyChanged.PropertyChanged"/> when the <c>CanExecute</c> property changes.</param>
    /// <param name="canExecutePropertyName">The name of the property that controls <c>CanExecute</c>.</param>
    /// <param name="supportsConcurrentExecution">If <c>true</c>, allows multiple concurrent executions of the command.</param>
    /// <returns>A new <see cref="AsyncDelegateCommand"/> instance that executes on a background thread and automatically raises <see cref="System.Windows.Input.ICommand.CanExecuteChanged"/> when the specified property changes.</returns>
    public static AsyncDelegateCommand CreateBackgroundDelegateCommand(
        Action<CancellationToken> execute,
        Func<bool> canExecute,
        INotifyPropertyChanged canExecuteNotifier,
        string canExecutePropertyName,
        bool supportsConcurrentExecution )
        => new( RunInBackground( execute ), canExecute, canExecuteNotifier, canExecutePropertyName, true, supportsConcurrentExecution );

    /// <summary>
    /// Creates a background command that accepts a parameter, with cancellation support and <see cref="INotifyPropertyChanged"/> integration. The execution is always dispatched to a background thread.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <param name="execute">The synchronous delegate to invoke on a background thread. Receives the parameter and a <see cref="CancellationToken"/>.</param>
    /// <param name="canExecute">A delegate that determines whether the command can execute for a given parameter.</param>
    /// <param name="canExecuteNotifier">The object that raises <see cref="INotifyPropertyChanged.PropertyChanged"/> when the <c>CanExecute</c> property changes.</param>
    /// <param name="canExecutePropertyName">The name of the property that controls <c>CanExecute</c>.</param>
    /// <param name="supportsConcurrentExecution">If <c>true</c>, allows multiple concurrent executions of the command.</param>
    /// <returns>A new <see cref="AsyncDelegateCommand{T}"/> instance that executes on a background thread and automatically raises <see cref="System.Windows.Input.ICommand.CanExecuteChanged"/> when the specified property changes.</returns>
    public static AsyncDelegateCommand<T> CreateBackgroundDelegateCommand<T>(
        Action<T, CancellationToken> execute,
        Func<T, bool> canExecute,
        INotifyPropertyChanged canExecuteNotifier,
        string canExecutePropertyName,
        bool supportsConcurrentExecution )
        => new( RunInBackground( execute ), canExecute, canExecuteNotifier, canExecutePropertyName, true, supportsConcurrentExecution );

    /// <summary>
    /// Creates a background command without a parameter, without cancellation support, with <see cref="INotifyPropertyChanged"/> integration. The execution is always dispatched to a background thread.
    /// </summary>
    /// <param name="execute">The synchronous delegate to invoke on a background thread.</param>
    /// <param name="canExecute">A delegate that determines whether the command can execute.</param>
    /// <param name="canExecuteNotifier">The object that raises <see cref="INotifyPropertyChanged.PropertyChanged"/> when the <c>CanExecute</c> property changes.</param>
    /// <param name="canExecutePropertyName">The name of the property that controls <c>CanExecute</c>.</param>
    /// <param name="supportsConcurrentExecution">If <c>true</c>, allows multiple concurrent executions of the command.</param>
    /// <returns>A new <see cref="AsyncDelegateCommand"/> instance that executes on a background thread and automatically raises <see cref="System.Windows.Input.ICommand.CanExecuteChanged"/> when the specified property changes.</returns>
    public static AsyncDelegateCommand CreateBackgroundDelegateCommand(
        Action execute,
        Func<bool> canExecute,
        INotifyPropertyChanged canExecuteNotifier,
        string canExecutePropertyName,
        bool supportsConcurrentExecution )
        => new( RunInBackground( _ => execute() ), canExecute, canExecuteNotifier, canExecutePropertyName, false, supportsConcurrentExecution );

    /// <summary>
    /// Creates a background command that accepts a parameter, without cancellation support, with <see cref="INotifyPropertyChanged"/> integration. The execution is always dispatched to a background thread.
    /// </summary>
    /// <typeparam name="T">The type of the command parameter.</typeparam>
    /// <param name="execute">The synchronous delegate to invoke on a background thread.</param>
    /// <param name="canExecute">A delegate that determines whether the command can execute for a given parameter.</param>
    /// <param name="canExecuteNotifier">The object that raises <see cref="INotifyPropertyChanged.PropertyChanged"/> when the <c>CanExecute</c> property changes.</param>
    /// <param name="canExecutePropertyName">The name of the property that controls <c>CanExecute</c>.</param>
    /// <param name="supportsConcurrentExecution">If <c>true</c>, allows multiple concurrent executions of the command.</param>
    /// <returns>A new <see cref="AsyncDelegateCommand{T}"/> instance that executes on a background thread and automatically raises <see cref="System.Windows.Input.ICommand.CanExecuteChanged"/> when the specified property changes.</returns>
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