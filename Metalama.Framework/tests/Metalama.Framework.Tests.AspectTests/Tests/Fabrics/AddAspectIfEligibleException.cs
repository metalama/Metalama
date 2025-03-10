// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Fabrics.AddAspectIfEligibleException;

class MyAspect : TypeAspect
{
    public override void BuildEligibility( IEligibilityBuilder<INamedType> builder )
    {
        throw new Exception();
    }
}

class Fabric : ProjectFabric
{
    public override void AmendProject(IProjectAmender amender)
    {
        amender.SelectTypes().AddAspectIfEligible<MyAspect>();
    }
}

// <target>
public class Target { }