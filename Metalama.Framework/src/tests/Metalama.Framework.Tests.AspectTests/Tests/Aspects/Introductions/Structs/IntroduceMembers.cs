// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Structs.IntroduceMembers;

public interface IHasLabel
{
    string Label { get; }
}

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // A readonly struct that receives an automatic property is the example of section 1 of
        // Metalama.Framework/docs/introducing-structs.md. Introducing a property into a struct also replaces the
        // implicit parameterless constructor of that struct, which is the member that section 4.2 of
        // introducing-types.md registers and that nothing emits.
        var introducedStruct = builder.IntroduceStruct(
            "ReadOnlyStruct",
            buildType: t =>
            {
                t.Accessibility = Accessibility.Public;
                t.IsReadOnly = true;
            } );

        var target = builder.With( introducedStruct.Declaration );

        target.IntroduceAutomaticProperty(
            "Label",
            typeof(string),
            buildProperty: p =>
            {
                p.Accessibility = Accessibility.Public;
                p.Writeability = Writeability.InitOnly;
            } );
        target.IntroduceMethod( nameof(MethodTemplate) );
        target.ImplementInterface( typeof(IHasLabel), OverrideStrategy.Ignore );
    }

    [Template]
    public int MethodTemplate() => 42;
}

// <target>
[Introduction]
public class TargetType { }
