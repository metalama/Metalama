// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Fabrics;
using Metalama.Patterns.Observability.Configuration;

namespace Metalama.Patterns.Observability.AspectTests.Options.IgnoreUnobservableExpressions.UsingFabricOnExternalClass;

public sealed class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender
            .SelectReflectionType( typeof(ExternalClass) )
            .ConfigureObservability( b => b.ObservabilityContract = ObservabilityContract.Constant );
    }
}

// <target>
[Observable]
public class UsingFabricOnExternalClass
{
    public int Count => ExternalClass.Foo();
}