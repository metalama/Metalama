// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.AspectOnLocalFunction;

internal class MethodAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Hello, world." );

        return meta.Proceed();
    }
}

internal class MethodBaseAspect : Attribute, IAspect<IMethodBase>
{
    public void BuildAspect( IAspectBuilder<IMethodBase> builder ) { }

    public void BuildEligibility( IEligibilityBuilder<IMethodBase> builder ) { }
}

internal class Contract : ContractAspect
{
    public override void Validate( dynamic? value ) { }
}

internal class TargetCode
{
    private int Method( int a )
    {
        [MethodAspect]
        [MethodBaseAspect]
        [return: Contract]
        int LocalFunction( [Contract] int a ) => a;

        LocalFunction( a );

        return a;
    }
}