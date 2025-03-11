// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Observability.AspectTests.ChildPropertiesFourDeep;

[Observable]
public partial class A
{
    public A()
    {
        this.A2 = new B();
    }

    public int A1 { get; set; }

    public B A2 { get; set; }

    public int A3 => this.A2.B2.C2.D1;
}

[Observable]
public partial class B
{
    public B()
    {
        this.B2 = new C();
    }

    public int B1 { get; set; }

    public C B2 { get; set; }
}

[Observable]
public partial class C
{
    public C()
    {
        this.C2 = new D();
    }

    public int C1 { get; set; }

    public D C2 { get; set; }
}

[Observable]
public partial class D
{
    public int D1 { get; set; }

    public int D2 { get; set; }
}