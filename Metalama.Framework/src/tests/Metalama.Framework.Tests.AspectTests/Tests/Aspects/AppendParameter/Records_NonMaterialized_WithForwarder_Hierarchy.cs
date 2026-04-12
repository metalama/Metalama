// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Records_NonMaterialized_WithForwarder_Hierarchy;

/*
 * Verifies the forwarding-constructor path through a record hierarchy when the introduced
 * parameter is non-materialized: each record gets a forwarder preserving its pre-mutation
 * arity, and the derived record's materialized body constructor threads the appended
 * parameter via :base(...). The appended parameter must not appear as a positional property
 * or in any Deconstruct.
 */

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var constructor in builder.Target.Constructors )
        {
            if ( constructor.IsRecordCopyConstructor() )
            {
                continue;
            }

            builder.With( constructor )
                .IntroduceParameter(
                    "p",
                    typeof(int),
                    pullStrategy: PullStrategy.IntroduceParameterAndPull(),
                    overloadingStrategy: ConstructorOverloadingStrategy.ForwardSourceConstructors );
        }
    }
}

// <target>
[MyAspect]
public record Base( int X );

// <target>
public record Derived( int X, int Y ) : Base( X );