// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AsField;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var result = builder.IntroduceClass(
            "IntroducedNestedType",
            buildType: t => { t.Accessibility = Code.Accessibility.Public; } );

        var existingNested = builder.Target.Types.Single();

        builder.IntroduceField(
            nameof(FieldTemplate),
            buildField: b =>
            {
                b.Name = "FieldWithIntroduced";
                b.Type = result.Declaration;
            } );

        builder.IntroduceField(
            nameof(FieldTemplate),
            buildField: b =>
            {
                b.Name = "FieldWithExisting";
                b.Type = existingNested;
            } );
    }

    [Template]
    public object? FieldTemplate;
}

#pragma warning disable CS8618

// <target>
[IntroductionAttribute]
public class TargetType
{
    public class ExistingNestedType { }
}