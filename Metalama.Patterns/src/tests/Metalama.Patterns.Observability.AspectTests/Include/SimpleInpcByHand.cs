// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.ComponentModel;

namespace Metalama.Patterns.Observability.AspectTests.Include;

/// <summary>
/// A simple hand-written class implementing <see cref="INotifyPropertyChanged"/>.
/// </summary>
public class SimpleInpcByHand : INotifyPropertyChanged
{
    public SimpleInpcByHand() { }

    public SimpleInpcByHand( int a )
    {
        this._a = a;
    }

    private int _a;

    public int A
    {
        get => this._a;
        set
        {
            if ( value != this._a )
            {
                this._a = value;
                this.PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof(this.A) ) );
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}