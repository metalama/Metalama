// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.ComponentModel;

namespace Metalama.Patterns.Observability.AspectTests.IntermediateInterface_ExplicitImpl;

public interface IMyInterface : INotifyPropertyChanged
{
    int P { get; }
}

public class C : IMyInterface
{
    private event PropertyChangedEventHandler? PropertyChanged;

    event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
    {
        add => this.PropertyChanged += value;
        remove => this.PropertyChanged -= value;
    }

    public int P => 0;
}

[Observable]
public class D
{
    public C C { get; set; }

    public int P => this.C.P;
}