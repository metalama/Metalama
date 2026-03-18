// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Eligibility.DelegateParameter;

[AttributeUsage( AttributeTargets.Parameter | AttributeTargets.ReturnValue )]
internal class MyAspect : Attribute, IAspect<IParameter>
{
    public void BuildAspect( IAspectBuilder<IParameter> builder ) { }

    public void BuildEligibility( IEligibilityBuilder<IParameter> builder ) { }
}

// <target>
[return: MyAspect]
public delegate int D( [MyAspect] int x );
