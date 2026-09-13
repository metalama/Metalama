// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.AddInitializer_Positional;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // An initializer that runs before an instance constructor replaces the primary constructor of the record
        // with an explicit one, exactly as it does for a record read from source, which
        // Initialization/BeforeInstanceConstructor_Record_Primary covers. The declaration therefore loses its
        // positional parameter list and gains the positional property, the Deconstruct method and the constructor
        // that runs the initializer.
        var positional = builder.IntroduceRecord(
            "Positional",
            RecordKind.Class,
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
