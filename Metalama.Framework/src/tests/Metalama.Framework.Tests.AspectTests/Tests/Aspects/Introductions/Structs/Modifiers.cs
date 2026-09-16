// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Structs.Modifiers;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceStruct(
            "ReadOnlyStruct",
            buildType: t =>
            {
                t.Accessibility = Accessibility.Public;
                t.IsReadOnly = true;
            } );

        builder.IntroduceStruct(
            "RefStruct",
            buildType: t =>
            {
                t.Accessibility = Accessibility.Public;
                t.IsRef = true;
            } );

        // The language orders readonly before ref, and both before partial.
        builder.IntroduceStruct(
            "ReadOnlyRefPartialStruct",
            buildType: t =>
            {
                t.Accessibility = Accessibility.Public;
                t.IsReadOnly = true;
                t.IsRef = true;
                t.IsPartial = true;
            } );
    }
}

// <target>
[Introduction]
public class TargetType { }
