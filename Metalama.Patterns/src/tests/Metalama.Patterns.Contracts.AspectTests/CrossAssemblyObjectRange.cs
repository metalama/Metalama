// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Contracts.AspectTests.CrossAssemblyObjectRange;

internal class C : IValidated
{
    public object M( object a, object b, out object c )
    {
        c = a;

        return b;
    }
}