// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Observability.UnitTests.Assets.Core;
using System.ComponentModel;

namespace Metalama.Patterns.Observability.UnitTests.Assets.Generic;

/* Currently supported generic property types:
 * 
 * where class
 * where class, INotifyPropertyChanged
 * where struct
 * 
 * Not supported, probably never will(?):
 * 
 * where struct, INotifyPropertyChanged
 * unconstrained
 */

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
public partial class B<T>
    where T : class
{
    public T B1 { get; set; }
}

[Observable]
public partial class C<T>
    where T : struct
{
    public T C1 { get; set; }
}

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