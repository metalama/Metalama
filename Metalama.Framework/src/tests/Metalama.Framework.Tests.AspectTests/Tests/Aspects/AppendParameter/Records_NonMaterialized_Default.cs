// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Records_NonMaterialized_Default;

/*
 * Verifies the default (materializeOnRecord: false) behaviour when appending a parameter
 * to a record's primary constructor: the appended parameter should live as a constructor
 * parameter only — it must NOT become a positional property, must NOT appear in the
 * compiler-generated Deconstruct, and must NOT participate in Equals/ToString. The linker
 * strips the primary header and emits a body-declared constructor.
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
                    PullStrategy.IntroduceParameterAndPull() );
        }
    }
}

// <target>
[MyAspect]
public record R( int X );