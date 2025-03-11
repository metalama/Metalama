// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Contracts.AspectTests.Diagnostics.Invariant_Suspend_NotEnabled;

public class BaseClass
{
    [Invariant]
    private void TheInvariant()
    {
        if ( this.A + this.B != 0 )
        {
            throw new InvariantViolationException();
        }
    }

    public int A { get; set; }

    public int B { get; set; }

    [SuspendInvariants]
    public void ExecuteWithoutInvariants()
    {
        this.A = -5;
        this.B = 5;
    }
}