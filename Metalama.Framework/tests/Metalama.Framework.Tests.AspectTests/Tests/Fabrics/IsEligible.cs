// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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