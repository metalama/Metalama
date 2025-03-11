// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.ComponentModel;
using System.Windows.Input;

namespace Metalama.Patterns.Wpf;

/// <summary>
/// An implementation of <see cref="ICommand"/> which uses delegates to access callbacks, accepting a parameter.
/// </summary>
[PublicAPI]
public sealed class DelegateCommand<T> : BaseDelegateCommand
{
    private readonly Func<T, bool>? _canExecute;
    private readonly Action<T> _execute;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateCommand{T}"/> class, without <see cref="INotifyPropertyChanged"/> integration.
    /// </summary>
    internal DelegateCommand( Action<T> execute, Func<T, bool>? canExecute )
    {
        this._execute = execute;
        this._canExecute = canExecute;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateCommand{T}"/> class, with <see cref="INotifyPropertyChanged"/> integration.
    /// </summary>
    internal DelegateCommand(
        Action<T> execute,
        Func<T, bool> canExecute,
        INotifyPropertyChanged canExecutePropertyChangeNotifier,
        string canExecutePropertyName ) : base( canExecutePropertyChangeNotifier, canExecutePropertyName )
    {
        this._execute = execute;
        this._canExecute = canExecute;
    }

    /// <summary>
    /// Executes the command with a given parameter.
    /// </summary>
    public void Execute( T parameter )
    {
        if ( !this.CanExecute( parameter ) )
        {
            throw new InvalidOperationException( "Command cannot be executed." );
        }

        this._execute( parameter );
    }

    /// <summary>
    /// Determines if the <see cref="Execute"/> method can be called with a given parameter.
    /// </summary>
    public bool CanExecute( T parameter ) => this._canExecute == null || this._canExecute( parameter );

    private protected override bool CanExecuteCore( object? parameter )
    {
        if ( parameter is not T value )
        {
            throw new ArgumentOutOfRangeException( nameof(parameter) );
        }

        return this.CanExecute( value );
    }

    private protected override void ExecuteCore( object? parameter ) => this.Execute( (T) parameter! );
}