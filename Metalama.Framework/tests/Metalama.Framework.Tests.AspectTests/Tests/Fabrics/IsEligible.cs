// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.IsEligible;

public class MyAspect : OverrideMethodAspect
{
    public override void BuildEligibility( IEligibilityBuilder<IMethod> builder )
    {
        base.BuildEligibility( builder );
        builder.MustNotBeStatic();
    }

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Overridden." );

        return default;
    }
}

public class TopLevelAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.Outbound.SelectMany( t => t.Methods.Where( m => m.IsAspectEligible<MyAspect>() ) ).AddAspect<MyAspect>();
    }
}

// <target>
[TopLevelAspect]
internal class C
{
    public void EligibleMethod() { }

    public static void NonEligibleMethod() { }
}