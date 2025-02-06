// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Observability.AspectTests.Diagnostics.CallUnsafeMethodOfExternalClass;

#if TEST_OPTIONS
// @RemoveOutputCode
#endif

[Observable]
public class CallUnsafeMethodOfExternalClass
{
    public int Count => ExternalClass.Foo( this );
}