// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug34319;

public class IntroduceParametersAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        for (var i = 0; i < 3; i++)
        {
            builder.With( builder.Target.Constructors.Single() ).IntroduceParameter( $"p{i}", typeof(int), TypedConstant.Create( 0 ) );
        }
    }
}

// <target>
[IntroduceParameters]
internal class TargetWithoutConstructor { }

// <target>
[IntroduceParameters]
internal class TargetWithConstructor
{
    public TargetWithConstructor( string s ) { }
}