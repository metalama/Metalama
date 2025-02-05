// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug34547;

public class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender
            .SelectMany( compilation => compilation.AllTypes )
            .Where( type => type.Accessibility is Accessibility.Public )
            .SelectMany( type => type.Methods )
            .Where( method => method.Accessibility is Accessibility.Public )
            .AddAspectIfEligible<LogAttribute>();
    }
}

public class LogAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        return meta.Proceed();
    }
}

// <target>
public delegate void D();