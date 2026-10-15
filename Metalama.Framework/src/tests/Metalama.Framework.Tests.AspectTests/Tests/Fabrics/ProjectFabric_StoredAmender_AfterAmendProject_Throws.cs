// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Fabrics.ProjectFabric_StoredAmender_AfterAmendProject_Throws;

// The amender of the fabric is used by an aspect, after AmendProject has returned.
// Adding a child aspect through it must fail, because the contributor would not be part of the result of the fabric.

internal class Fabric : ProjectFabric
{
    public static IProjectAmender? StoredAmender;

    public override void AmendProject( IProjectAmender amender )
    {
        StoredAmender = amender;

        amender.SelectTypes().Where( t => t.Name == "C" ).AddAspect<LateAspect>();
    }
}

internal class LateAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        Fabric.StoredAmender!.SelectTypes().Where( t => t.Name == "C" ).SelectMany( t => t.Methods ).AddAspect<ChildAspect>();
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
