// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;

namespace Metalama.Patterns.Contracts.AspectTests.DisablePostconditions;

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
        amender.SetOptions( new ContractOptions { ArePostconditionsEnabled = false } );
    }
}