// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Records_NonMaterialized_WithForwarder;

/*
 * Verifies the forwarding-constructor path on a sealed record when the introduced parameter is
 * non-materialized: the linker strips the primary header, emits the materialized body ctor
 * carrying the appended parameter, and an aspect-generated forwarder preserving
 * the pre-mutation arity-1 signature. The appended parameter must NOT appear as a property
 * or in the Deconstruct.
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
public sealed record R( int X );