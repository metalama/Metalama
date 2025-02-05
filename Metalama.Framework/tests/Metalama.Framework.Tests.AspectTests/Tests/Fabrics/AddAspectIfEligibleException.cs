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