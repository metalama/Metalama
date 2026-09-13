// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.AddInitializer_PositionalRecordStruct;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // The record struct is the second authoring form of the replacement that AddInitializer_Positional covers on
        // a record class. A record struct has a parameterless constructor of its own, because every struct has one,
        // so the declaration carries it beside the constructor that takes the positional parameter.
        var positional = builder.IntroduceRecord(
            "Positional",
            RecordKind.Struct,
            buildRecord: r =>
            {
                r.Accessibility = Accessibility.Public;
                r.AddPositionalParameter( "Value", typeof(int) );
            } );

        builder.With( positional.Declaration ).AddInitializer( nameof(Template), InitializerKind.BeforeInstanceConstructor );
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( $"Initializing {meta.Target.Type.Name}." );
    }
}

// <target>
[Introduction]
public class TargetType { }
