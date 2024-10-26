// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.ComponentModel;
using System.Windows.Input;

namespace Metalama.Patterns.Wpf;

/// <summary>
/// A base class for delegate-based async commands.
/// </summary>
public abstract class BaseAsyncDelegateCommand : BaseDelegateCommand, IAsyncCommand
{
    private readonly bool _supportsCancellation;
    private readonly bool _supportsConcurrentExecution;
    private static readonly string[] _allProperties = [nameof(ExecutionTask), nameof(IsRunning), nameof(IsCancellationRequested), nameof(CanCancel)];
    private static readonly string[] _cancellationProperties = [nameof(CanCancel), nameof(IsCancellationRequested)];
    private CancellationTokenSource? _cancellationTokenSource;

    private protected BaseAsyncDelegateCommand(
        INotifyPropertyChanged canExecutePropertyChangeNotifier,
        string canExecutePropertyName,
        bool supportsCancellation,
        bool supportsConcurrentExecution ) : base( canExecutePropertyChangeNotifier, canExecutePropertyName )
    {
        this._supportsCancellation = supportsCancellation;
        this._supportsConcurrentExecution = supportsConcurrentExecution;
    }

    private protected BaseAsyncDelegateCommand( bool supportsCancellation, bool supportsConcurrentExecution )
    {
        this._supportsCancellation = supportsCancellation;
        this._supportsConcurrentExecution = supportsConcurrentExecution;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets a value indicating whether the current command can be cancelled. Returns <c>true</c> if the command
    /// supports cancellation (e.g. has a parameter of type <see cref="CancellationToken"/>) and is currently running.
    /// </summary>
    public bool CanCancel => this._supportsCancellation && this.IsRunning;

    /// <summary>
    /// Gets a value indicating whether the <see cref="Cancel"/> method was called for the last task.
    /// </summary>
    public bool IsCancellationRequested => this._cancellationTokenSource is { IsCancellationRequested: true };

    /// <summary>
    /// Gets a value indicating whether the last execution task (i.e., <see cref="ExecutionTask"/>) is still running.
    /// </summary>
    public bool IsRunning => this.ExecutionTask is { Status: not (TaskStatus.Canceled or TaskStatus.Faulted or TaskStatus.RanToCompletion) };

    /// <summary>
    /// Gets the <see cref="Task"/> representing the last execution of the command.
    /// </summary>
    public Task? ExecutionTask { get; private set; }

    /// <summary>
    /// Event raised when the <see cref="AsyncDelegateCommand{T}.Execute"/> method is called.
    /// </summary>
    public event Action<DelegateCommandExecution>? Executed;

    void ICommand.Execute( object? parameter ) => this.OuterExecute( parameter );

    bool ICommand.CanExecute( object? parameter ) => this.OuterCanExecute( parameter );

    private protected bool OuterCanExecute( object? parameter )
    {
        if ( this.IsRunning && !this._supportsConcurrentExecution )
        {
            return false;
        }
        else
        {
            return this.InnerCanExecute( parameter );
        }
    }

    private protected abstract bool InnerCanExecute( object? parameter );

    private protected sealed override bool CanExecuteCore( object? parameter ) => this.OuterCanExecute( parameter );

    private protected sealed override void ExecuteCore( object? parameter ) => this.OuterExecute( parameter );

    private protected abstract Task InnerExecuteAsync( object? parameter, CancellationToken cancellationToken );

    private protected DelegateCommandExecution OuterExecute( object? parameter )
    {
        if ( !this.OuterCanExecute( parameter ) )
        {
            throw new InvalidOperationException( "The command cannot be executed." );
        }

        CancellationTokenSource? cancellationTokenSource;

        if ( this._supportsCancellation )
        {
            this._cancellationTokenSource = cancellationTokenSource = new CancellationTokenSource();
        }
        else
        {
            cancellationTokenSource = null;
        }

        var task = this.ExecuteAsync( parameter, cancellationTokenSource );

        this.ExecutionTask = task;

        this.OnPropertiesChanged( _allProperties );

        var token = new DelegateCommandExecution( cancellationTokenSource, task );
        this.Executed?.Invoke( token );

        return token;
    }

    private async Task ExecuteAsync( object? parameter, CancellationTokenSource? cancellationTokenSource )
    {
        try
        {
            await this.InnerExecuteAsync( parameter, cancellationTokenSource?.Token ?? CancellationToken.None );
        }
        finally
        {
            if ( cancellationTokenSource != null )
            {
                cancellationTokenSource.Dispose();

                // If the current execution is still the last one, reset the current CancellationTokenSource.
                Interlocked.CompareExchange( ref this._cancellationTokenSource, null, cancellationTokenSource );
            }

            this.OnPropertiesChanged( _allProperties );
        }
    }

    /// <summary>
    /// Cancels all currently pending executions.
    /// </summary>
    public void Cancel()
    {
        // We don't throw any exception in case there is nothing to cancel to avoid races with the CanCancel property.
        if ( this._cancellationTokenSource != null )
        {
            this._cancellationTokenSource.Cancel();

            this.OnPropertiesChanged( _cancellationProperties );
        }
    }

    private void OnPropertiesChanged( string[] properties )
    {
        var propertyChangedEventHandler = this.PropertyChanged;

        if ( propertyChangedEventHandler != null )
        {
            this.SendNotification(
                () =>
                {
                    foreach ( var property in properties )
                    {
                        propertyChangedEventHandler( this, new PropertyChangedEventArgs( property ) );
                    }
                } );
        }
    }
}