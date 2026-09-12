// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.ErrorAttributeOnSynthesizedMember;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var record = builder.IntroduceRecord(
            "Positional",
            buildRecord: r =>
            {
                r.Accessibility = Accessibility.Public;
                r.AddPositionalParameter( "Value", typeof(int) );
            } );

        // PrintMembers is synthesized by the compiler from the record declaration and nothing emits syntax for it,
        // so there is no declaration on which to write the attribute.
        var printMembers = record.Declaration.Methods.OfName( "PrintMembers" ).Single();

        builder.With( printMembers ).IntroduceAttribute( AttributeConstruction.Create( typeof(ObsoleteAttribute) ) );
    }
}

// <target>
[Introduction]
public class TargetType { }
