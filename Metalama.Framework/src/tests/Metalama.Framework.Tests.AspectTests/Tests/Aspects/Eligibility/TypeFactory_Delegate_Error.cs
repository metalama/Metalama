// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Eligibility.TypeFactory_Delegate_Error;

internal class TestAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder ) { }

    public override void BuildEligibility( IEligibilityBuilder<INamedType> builder )
    {
        builder.AddRules( _ => { TypeFactory.GetType( typeof(RunTimeClass) ); } );
    }
}

internal class RunTimeClass { }

// <target>
[TestAspect]
internal class TargetClass { }