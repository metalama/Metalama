// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.AddInitializer_PositionalError;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // An initializer that runs before an instance constructor replaces the primary constructor by an explicit
        // one, and that replacement reads the declaration of the record from source, which an introduced record
        // does not have. The advice therefore reports LAMA0553 instead of failing inside the linker.
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
