// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.ComponentModel;

namespace Metalama.Patterns.Observability.AspectTests.Diagnostics;

public class ExistingInpcImplWithoutNPCMethod : INotifyPropertyChanged
{
    private int _ex1;

    public int EX1
    {
        get => this._ex1;
        set
        {
            if ( value != this._ex1 )
            {
                this._ex1 = value;
                this.PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof(this.EX1) ) );
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

// <target>
[Observable]
public partial class InheritsExistingInpcImplWithoutNpcMethod : ExistingInpcImplWithoutNPCMethod { }