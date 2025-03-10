// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.StructAspect;

internal struct Aspect : IAspect<IMethod>
{
    public void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override( nameof(OverrideMethod) );
    }

    public void BuildEligibility( IEligibilityBuilder<IMethod> builder ) { }

    [Template]
    private dynamic? OverrideMethod()
    {
        Console.WriteLine( "regular template" );

        return meta.Proceed();
    }
}