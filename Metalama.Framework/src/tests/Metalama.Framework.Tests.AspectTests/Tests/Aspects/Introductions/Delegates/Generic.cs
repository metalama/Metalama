// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Delegates.Generic;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // A delegate is, with an interface, one of the two kinds of type whose type parameters may declare
        // variance, and the type parameters belong to the type rather than to the Invoke method.
        builder.IntroduceDelegate(
            "Transformer",
            d =>
            {
                d.Accessibility = Accessibility.Public;

                var input = d.AddTypeParameter( "TInput" );
                input.Variance = VarianceKind.In;

                var output = d.AddTypeParameter( "TOutput" );
                output.Variance = VarianceKind.Out;

                d.ReturnType = output;
                d.AddParameter( "value", input );
            } );

        builder.IntroduceDelegate(
            "Consumer",
            d =>
            {
                d.Accessibility = Accessibility.Public;

                var item = d.AddTypeParameter( "TItem" );
                d.AddParameter( "item", item );
            } );
    }
}

// <target>
[Introduction]
public class TargetType { }
