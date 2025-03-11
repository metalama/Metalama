// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Classes.AsMethodReturnParameter;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var result = builder.IntroduceClass(
            "IntroducedNestedType",
            buildType: t => { t.Accessibility = Code.Accessibility.Public; } );

        var existingNested = builder.Target.Types.Single();

        builder.IntroduceMethod(
            nameof(MethodTemplate),
            buildMethod: b =>
            {
                b.Name = "MethodWithIntroduced";
                b.ReturnType = result.Declaration;
            } );

        builder.IntroduceMethod(
            nameof(MethodTemplate),
            buildMethod: b =>
            {
                b.Name = "MethodWithExisting";
                b.ReturnType = existingNested;
            } );
    }

    [Template]
    public dynamic? MethodTemplate()
    {
        return default;
    }
}

// <target>
[IntroductionAttribute]
public class TargetType
{
    public class ExistingNestedType { }
}