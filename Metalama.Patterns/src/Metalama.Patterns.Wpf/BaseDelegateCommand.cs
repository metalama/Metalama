// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.ComponentModel;
using System.Windows.Input;

namespace Metalama.Patterns.Wpf;

/// <summary>
/// A base class for all implementations of the <see cref="ICommand"/> interface in this package, based on delegates.
/// </summary>
public abstract class BaseDelegateCommand : ICommand
{
    private readonly SynchronizationContext? _synchronizationContext;

    private readonly string? _canExecutePropertyName;

    bool ICommand.CanExecute( object? parameter ) => this.CanExecuteCore( parameter );

    void ICommand.Execute( object? parameter ) => this.ExecuteCore( parameter );

    private protected abstract bool CanExecuteCore( object? parameter );

    private protected abstract void ExecuteCore( object? parameter );

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