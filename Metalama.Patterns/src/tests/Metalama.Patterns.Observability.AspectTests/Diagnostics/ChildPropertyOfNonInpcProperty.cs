// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @RemoveOutputCode
#endif

namespace Metalama.Patterns.Observability.AspectTests.Diagnostics.PropertyOfNonInpcProperty;

public class A
{
    public int A1 { get; set; }
}

public class B
{
    public A B1 { get; set; }
}

// <target>
[Observable]
public class C
{
    public B C1 { get; set; }

    public int C2 => this.C1.B1.A1;
}