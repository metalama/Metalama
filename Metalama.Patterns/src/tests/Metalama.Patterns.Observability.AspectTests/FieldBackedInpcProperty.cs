// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Metalama.Patterns.Observability.AspectTests.FieldBackedInpcProperty;

[Observable]
public class A
{
    public int A1 { get; set; }
}

// <target>
[Observable]
public class FieldBackedInpcProperty
{
    private A _x;

    public A P1 => this._x;

    public int P2 => this._x.A1;
}