// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Records_Materialized_OptIn;

/*
 * Verifies the opt-in (materializeOnRecord: true) behaviour when appending a parameter
 * to a record's primary constructor: the appended parameter keeps its position in the
 * primary header, becomes a positional property, and appears in the compiler-generated
 * Deconstruct. This is the pre-2026.1 default, preserved for aspects that deliberately
 * want the parameter to participate in the record's value identity.
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
                    TypedConstant.Create( 15 ),
                    PullStrategy.IntroduceParameterAndPull( materializeOnRecord: true ) );
        }
    }
}

// <target>
[MyAspect]
public record R( int X );