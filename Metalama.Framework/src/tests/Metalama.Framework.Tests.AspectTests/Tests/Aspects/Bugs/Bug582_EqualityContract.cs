// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug582_EqualityContract;

public class OverrideEqualityContractAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceProperty( nameof(EqualityContract), whenExists: OverrideStrategy.Override );
    }

    [Template( Name = "EqualityContract" )]
    protected virtual Type EqualityContract
    {
        get
        {
            Console.WriteLine( "Aspect code." );

            return meta.Proceed()!;
        }
    }
}

// <target>
[OverrideEqualityContractAttribute]
internal record Target;
