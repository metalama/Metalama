// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.ComponentModel;

namespace Metalama.Patterns.Observability.AspectTests;

[Observable]
public partial class A<T>
    where T : class, INotifyPropertyChanged
{
    public T A1 { get; set; }
}

public partial class AOfSimple : A<Simple>
{
    public int RefA1S1 => this.A1.S1;
}

[Observable]
public partial class Simple
{
    public int S1 { get; set; }

    public int S2 { get; set; }

    public int S3 { get; set; }
}

#if !METALAMA
public partial class Simple : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
}
#endif