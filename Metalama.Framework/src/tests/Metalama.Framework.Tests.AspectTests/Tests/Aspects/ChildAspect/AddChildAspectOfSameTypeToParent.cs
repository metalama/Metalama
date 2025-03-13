// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.AddChildAspectOfSameTypeToParent;

[AttributeUsage( AttributeTargets.Method )]
public class MyAspect : Aspect, IAspect<IMethod>, IAspect<INamedType>
{
    public void BuildAspect( IAspectBuilder<INamedType> builder ) { }

    public void BuildEligibility( IEligibilityBuilder<INamedType> builder ) { }

    public void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Outbound.Select( t => t.DeclaringType ).AddAspect( _ => this );
    }

    public void BuildEligibility( IEligibilityBuilder<IMethod> builder ) { }
}

// <target>

internal class C
{
    [MyAspect]
    private void M() { }
}