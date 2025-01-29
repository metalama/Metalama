// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;

namespace Metalama.Patterns.Contracts.AspectTests.Invariants_Disable;

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

public class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.SetOptions( new ContractOptions { AreInvariantsEnabled = false } );
    }
}