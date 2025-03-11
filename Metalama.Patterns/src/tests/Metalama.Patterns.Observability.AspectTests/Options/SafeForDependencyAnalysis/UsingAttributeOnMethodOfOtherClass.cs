// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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