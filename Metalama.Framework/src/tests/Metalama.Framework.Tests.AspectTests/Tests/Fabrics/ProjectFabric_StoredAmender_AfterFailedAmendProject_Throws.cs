// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Fabrics.ProjectFabric_StoredAmender_AfterFailedAmendProject_Throws;

// The first fabric stores its amender and throws. The amender is then used by an aspect that the second fabric adds.
// Adding a child aspect through the stored amender must fail, because the first fabric has no result.

internal class FailingFabric : ProjectFabric
{
    public static IProjectAmender? StoredAmender;

    public override void AmendProject( IProjectAmender amender )
    {
        StoredAmender = amender;

        throw new InvalidOperationException( "FailingFabric failed." );
    }
}

internal class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.SelectTypes().Where( t => t.Name == "C" ).AddAspect<LateAspect>();
    }
}

internal class LateAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        FailingFabric.StoredAmender!.SelectTypes().Where( t => t.Name == "C" ).SelectMany( t => t.Methods ).AddAspect<ChildAspect>();
    }
}

internal class ChildAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => meta.Proceed();
}

// <target>
internal class C
{
    private void M() { }
}
