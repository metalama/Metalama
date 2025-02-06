// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Observability.AspectTests.InheritFromAbstractBase;

public partial class C11 : C10
{
    /// <summary>
    /// Ref to <see cref="C10.C10P1"/>.
    /// </summary>
    public int C11P1 => this.C10P1;
}

[Observable]
public abstract partial class C10
{
    /// <summary>
    /// Auto
    /// </summary>
    public int C10P1 { get; set; }
}