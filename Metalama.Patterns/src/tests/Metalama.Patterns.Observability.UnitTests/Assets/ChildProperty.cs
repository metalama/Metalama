// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Observability.UnitTests.Assets.ChildPropertyAssets;

[Observable]
public partial class A
{
    public A()
    {
        this.A2 = new B();
    }

    public int A1 { get; set; }

    public B A2 { get; set; }

    /// <summary>
    /// Leaf ref (A2.B2.C2.D1).
    /// </summary>
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

[Observable]
public partial class E
{
    public E()
    {
        this.E2 = new B();
    }

    public int E1 { get; set; }

    public B E2 { get; set; }

    /// <summary>
    /// Leaf ref (E2.B2.C2.D1).
    /// </summary>
    public int LR => this.E2.B2.C2.D1;

    /// <summary>
    /// Leaf-parent ref (E2.B2.C1).
    /// </summary>
    public int LP1R => this.E2.B2.C1;

    /// <summary>
    /// Leaf-parent-parent ref (E2.B1).
    /// </summary>
    public int LP2R => this.E2.B1;
}

[Observable]
public partial class F
{
    public F()
    {
        this.F1 = new B();
    }

    /// <summary>
    /// Auto
    /// </summary>
    public B F1 { get; set; }

    /// <summary>
    /// Ref to F1.B2.
    /// </summary>
    public C F2 => this.F1.B2;
}

/// <summary>
/// G : F. Has ref to F1.B2.C2.D1 where the base class F only
/// has a ref to F1.B2 as a leaf, and no ref to C2 or deeper.
/// </summary>
public partial class G : F
{
    /// <summary>
    /// Ref to F1.B2.C2.D1. Note, class F does not register with F1.B2.
    /// </summary>
    public int G1 => this.F1.B2.C2.D1;
}