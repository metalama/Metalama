// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Observability.AspectTests.Options.IgnoreUnobservableExpressions.UsingAttributeOnMethodOfOtherClass;

public static class OtherClass
{
    [Constant]
    public static int Foo() => 42;
}

// <target>
[Observable]
public class UsingAttributeOnMethodOfOtherClass
{
    public int X => OtherClass.Foo();
}