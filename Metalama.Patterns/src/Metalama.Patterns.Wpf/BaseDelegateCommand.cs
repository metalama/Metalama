// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.ComponentModel;
using System.Windows.Input;

namespace Metalama.Patterns.Wpf;

/// <summary>
/// Base class for all delegate-based implementations of <see cref="ICommand"/> in this package.
/// Provides common functionality for command execution and <see cref="INotifyPropertyChanged"/> integration.
/// </summary>
/// <remarks>
/// <para>This class handles the <see cref="CanExecuteChanged"/> event and integrates with <see cref="INotifyPropertyChanged"/>
/// to automatically raise <see cref="CanExecuteChanged"/> when a bound <c>CanExecute</c> property changes.</para>
/// <para>Notifications are dispatched through the <see cref="SynchronizationContext"/> captured at construction time
/// to ensure thread-safe UI updates.</para>
/// </remarks>
/// <seealso cref="DelegateCommand"/>
/// <seealso cref="DelegateCommand{T}"/>
/// <seealso cref="BaseAsyncDelegateCommand"/>
/// <seealso href="@wpf-command"/>
public abstract class BaseDelegateCommand : ICommand
{
    private readonly SynchronizationContext? _synchronizationContext;

    private readonly string? _canExecutePropertyName;

    bool ICommand.CanExecute( object? parameter ) => this.CanExecuteCore( parameter );

    void ICommand.Execute( object? parameter ) => this.ExecuteCore( parameter );

    private protected abstract bool CanExecuteCore( object? parameter );

    private protected abstract void ExecuteCore( object? parameter );

    /// <summary>
    /// Occurs when the return value of <see cref="ICommand.CanExecute"/> may have changed.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    private protected BaseDelegateCommand( INotifyPropertyChanged canExecutePropertyChangeNotifier, string canExecutePropertyName )
    {
        this._synchronizationContext = SynchronizationContext.Current;
        this._canExecutePropertyName = canExecutePropertyName;
        canExecutePropertyChangeNotifier.PropertyChanged += this.OnUpstreamPropertyChanged;
    }

    private protected BaseDelegateCommand()
    {
        this._synchronizationContext = SynchronizationContext.Current;
    }

    private void OnUpstreamPropertyChanged( object? sender, PropertyChangedEventArgs args )
    {
        if ( this.CanExecuteChanged != null )
        {
            if ( args.PropertyName == this._canExecutePropertyName )
            {
                if ( this._synchronizationContext != null )
                {
                    this.SendNotification( this.OnCanExecuteChanged );
                }
                else
                {
                    this.OnCanExecuteChanged();
                }
            }
        }
    }

    private protected void SendNotification( Action action )
    {
        if ( this._synchronizationContext != null )
        {
            // Send the message asynchronously (do not wait).
            this._synchronizationContext.Post( static a => ((Action) a!)(), action );
        }
        else
        {
            action();
        }
    }

    private protected virtual void OnCanExecuteChanged()
    {
        this.CanExecuteChanged?.Invoke( this, EventArgs.Empty );
    }
}