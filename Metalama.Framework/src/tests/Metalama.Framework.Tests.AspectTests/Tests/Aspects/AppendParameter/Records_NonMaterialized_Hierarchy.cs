// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Records_NonMaterialized_Hierarchy;

/*
 * Verifies the default (materializeOnRecord: false) behaviour through a record hierarchy:
 * the appended parameter must be threaded via :base(...) from the derived record's body
 * constructor to the base record's body constructor, but must NOT appear in either
 * record's value shape (no properties, no Deconstruct entries).
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
public record Base( int X );

// <target>
public record Derived( int X, int Y ) : Base( X );