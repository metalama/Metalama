// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;

namespace Metalama.Patterns.Contracts.AspectTests.DisablePreconditions;

public class C
{
    [NotEmpty( Direction = ContractDirection.Both )]
    public string P { get; set; } = "x";

    public void M( [NotEmpty] string a, [NotEmpty] out string b )
    {
        b = "b";
    }
}

public class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.SetOptions( new ContractOptions { ArePreconditionsEnabled = false } );
    }
}