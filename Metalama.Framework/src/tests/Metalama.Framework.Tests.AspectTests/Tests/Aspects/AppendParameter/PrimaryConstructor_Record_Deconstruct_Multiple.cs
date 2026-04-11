// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.PrimaryConstructor_Record_Deconstruct_Multiple;

/*
 * Tests that when multiple parameters are introduced into a record's primary constructor,
 * a Deconstruct overload for the original signature is generated (#698).
 */

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var constructor in builder.Target.Constructors)
        {
            if (constructor.IsRecordCopyConstructor())
            {
                continue;
            }

            builder.With( constructor )
                .IntroduceParameter(
                    "introduced1",
                    typeof(int),
                    TypedConstant.Create( 0 ),
                    PullStrategy.IntroduceParameterAndPull( materializeOnRecord: true ) );

            builder.With( constructor )
                .IntroduceParameter(
                    "introduced2",
                    typeof(int),
                    TypedConstant.Create( 0 ),
                    PullStrategy.IntroduceParameterAndPull( materializeOnRecord: true ) );
        }
    }
}

// <target>
[MyAspect]
public record R( int X, int Y )
{
    public void M()
    {
        var (x, y) = this;
    }
}
