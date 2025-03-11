// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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