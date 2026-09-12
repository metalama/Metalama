// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.AddInitializer_NonPositional;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // A record that declares no positional parameter has the implicit parameterless constructor and no primary
        // one, so the initializer replaces that implicit constructor in the way it does for an introduced class.
        var record = builder.IntroduceRecord(
            "NonPositional",
            RecordKind.Class,
            buildRecord: r => r.Accessibility = Accessibility.Public );

        builder.With( record.Declaration ).AddInitializer( nameof(Template), InitializerKind.BeforeInstanceConstructor );
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
