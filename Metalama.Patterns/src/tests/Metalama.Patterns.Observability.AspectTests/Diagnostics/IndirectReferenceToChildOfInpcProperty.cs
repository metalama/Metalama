// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Observability.AspectTests.IndirectReferenceToChildOfInpcProperty;

#if TEST_OPTIONS
// @RemoveOutputCode
#endif

[Observable]
public class A
{
    public int A1 { get; set; }
}

// <target>
[Observable]
public class IndirectReferenceToChildOfInpcProperty
{
    public A P1 { get; set; }

    public A P2 => this.P1;

    // P3 depends on a child of P1 indirectly through P2.
    // At the time of writing, we don't notice that a change to P1.A1 results in a change to P3.
    // For now, we deal with this by warning on access to 'P2' (it's an non-auto INPC property of
    // the target type).
    public int P3 => this.P2.A1;
}