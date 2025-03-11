// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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