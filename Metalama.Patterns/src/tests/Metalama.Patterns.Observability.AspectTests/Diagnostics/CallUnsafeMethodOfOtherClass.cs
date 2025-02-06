// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Observability.AspectTests.Diagnostics.CallUnsafeMethodOfOtherClass;

#if TEST_OPTIONS
// @RemoveOutputCode
#endif

public static class OtherClass
{
    public static int Foo( object x ) => 42;
}

[Observable]
public class CallUnsafeMethodOfOtherClass
{
    public int X => OtherClass.Foo( this );
}