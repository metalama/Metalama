// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Observability.AspectTests.Diagnostics.CallUnsafeMethodOfExternalClass;

public static class ExternalClass
{
    public static int Foo( object x ) => x.GetType().Name.Length;
}