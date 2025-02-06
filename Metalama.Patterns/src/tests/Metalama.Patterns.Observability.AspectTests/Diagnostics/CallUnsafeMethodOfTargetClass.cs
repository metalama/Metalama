// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Observability.AspectTests.Diagnostics.CallUnsafeMethodOfTargetClass;

#if TEST_OPTIONS
// @RemoveOutputCode
#endif

[Observable]
public class CallUnsafeMethodOfTargetClass
{
    public int X => this.Foo();

    // ReSharper disable once MemberCanBeMadeStatic.Local
    private int Foo() => 42;
}