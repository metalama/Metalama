// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.ComponentModel;

namespace Metalama.Patterns.Observability.AspectTests.Generic2;

public interface IFoo
{
    int X { get; }

    int Y { get; }
}

[Observable]
public partial class D<T>
    where T : class, INotifyPropertyChanged, IFoo
{
    public T D1 { get; set; }

    public int FooX => this.D1.X;
}

public partial class DD<T> : D<T>
    where T : class, INotifyPropertyChanged, IFoo
{
    public int FooY => this.D1.Y;
}

[Observable]
public partial class MyFoo : IFoo
{
    public int X { get; set; }

    public int Y { get; set; }
}