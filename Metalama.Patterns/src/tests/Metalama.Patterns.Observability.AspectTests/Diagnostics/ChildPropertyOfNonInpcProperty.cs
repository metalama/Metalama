// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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