// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Metalama.Patterns.Observability.AspectTests.ObservableRecipientBase;

public class A : ObservableRecipient
{
    private string _p;

    public string P
    {
        get => this._p;
        set => this.SetProperty( ref this._p, value );
    }
}

[Observable]
public class B : A
{
    public string Q => this.P;
}