// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Fabrics;

namespace Metalama.Patterns.Contracts.AspectTests.Ambiguities.Unspecified_Strict;

public class C
{
    [Positive]
    public int A { get; set; }

    [Negative]
    public int B { get; set; }

    [GreaterThan( 5 )]
    public int D { get; set; }

    [LessThan( 5 )]
    public int E { get; set; }
}

internal class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
        => amender.ConfigureContracts( new ContractOptions() { DefaultInequalityStrictness = InequalityStrictness.Strict } );
}