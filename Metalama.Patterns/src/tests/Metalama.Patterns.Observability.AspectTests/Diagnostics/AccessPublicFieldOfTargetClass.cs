// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Observability.AspectTests.Diagnostics.AccessPublicFieldOfTargetClass;

#if TEST_OPTIONS
// @RemoveOutputCode
#endif

[Observable]
public class AccessPublicFieldOfTargetClass
{
    public int _x;

    public int X => this._x;
}