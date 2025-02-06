// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.ComponentModel;

namespace Metalama.Patterns.Observability.AspectTests.IntermediateInterface;

public interface IMyInterface : INotifyPropertyChanged
{
    int P { get; }
}

public class C : IMyInterface
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public int P => 0;
}

[Observable]
public class D
{
    public IMyInterface C { get; set; }

    public int P => this.C.P;
}