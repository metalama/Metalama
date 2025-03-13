// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.AddChildAspectOfSameTypeToChild;

[AttributeUsage( AttributeTargets.Class )]
public class MyAspect : OverrideMethodAspect, IAspect<INamedType>
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Oops" );

        return meta.Proceed();
    }

    public void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.Outbound.SelectMany( t => t.Methods ).AddAspectIfEligible( _ => this );
    }

    public void BuildEligibility( IEligibilityBuilder<INamedType> builder ) { }
}

// <target>
[MyAspect]
internal class C
{
    private void M() { }
}