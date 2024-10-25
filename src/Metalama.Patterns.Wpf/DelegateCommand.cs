// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System.ComponentModel;

namespace Metalama.Patterns.Wpf;

[PublicAPI]
public sealed class DelegateCommand : BaseDelegateCommand, INotifyPropertyChanged
{
    private readonly Func<bool>? _canExecute;
    private readonly Action _execute;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateCommand"/> class, without <see cref="INotifyPropertyChanged"/> integration.
    /// </summary>
    internal DelegateCommand( Action execute, Func<bool>? canExecute )
    {
        this._execute = execute;
        this._canExecute = canExecute;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateCommand"/> class, with <see cref="INotifyPropertyChanged"/> integration.
    /// </summary>
    internal DelegateCommand(
        Action execute,
        Func<bool> canExecute,
        INotifyPropertyChanged canExecutePropertyChangeNotifier,
        string canExecutePropertyName ) : base( canExecutePropertyChangeNotifier, canExecutePropertyName )
    {
        this._execute = execute;
        this._canExecute = canExecute;
    }

    [Obsolete( "This command does not accept any arguments." )]
    public void Execute( object? parameter ) => this.Execute();

    public void Execute()
    {
        if ( !this.CanExecute )
        {
            throw new InvalidOperationException( "Command cannot be executed." );
        }

        this._execute();
    }

    public bool CanExecute => this._canExecute == null || this._canExecute();

    private protected override bool CanExecuteCore( object? parameter )
    {
        if ( parameter != null )
        {
            throw new ArgumentOutOfRangeException( nameof(parameter), "The parameter must be null." );
        }

        return this.CanExecute;
    }

    private protected override void ExecuteCore( object? parameter )
    {
        if ( parameter != null )
        {
            throw new ArgumentOutOfRangeException( nameof(parameter), "The parameter must be null." );
        }

        this.Execute();
    }

    private protected override void OnCanExecuteChanged()
    {
        base.OnCanExecuteChanged();
        this.PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof(this.CanExecute) ) );
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}