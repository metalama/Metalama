// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Fabrics;

namespace Metalama.Patterns.Contracts.UnitTests;

// ReSharper disable once UnusedType.Global
internal class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
        => amender.ConfigureContracts( new ContractOptions { DefaultInequalityStrictness = InequalityStrictness.NonStrict } );
}