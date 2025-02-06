// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Observability.AspectTests.Options.IgnoreUnobservableExpressions.UsingAttributeOnMethodOfTargetClass;

[Observable]
public class UsingAttributeOnMethodOfTargetClass
{
    public int X => this.Foo();

    [Constant]
    private int Foo() => 42;
}