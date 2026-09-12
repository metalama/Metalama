// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Structs.IntroduceField;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introducing a field into a struct replaces the implicit parameterless constructor of that struct, so this
        // test also covers the constructor that is registered in the code model and emitted by nothing.
        var result = builder.IntroduceStruct( "TestNestedType" );

        builder.With( result.Declaration ).IntroduceField( nameof(Field) );
    }

    [Template]
    public int Field;
}

// <target>
[Introduction]
public class TargetType { }
