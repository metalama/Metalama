// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug812_KeywordEscaping_AllMembers;

// Issue #812: Tests introduction of all member kinds with C# keyword names

internal class IntroduceAllKeywordMembersAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce field
        builder.IntroduceField( nameof(FieldTemplate), buildField: f => f.Name = "const" );

        // Introduce property
        builder.IntroduceProperty( nameof(PropertyTemplate), buildProperty: p => p.Name = "int" );

        // Introduce method
        builder.IntroduceMethod( nameof(MethodTemplate), buildMethod: m => m.Name = "void" );

        // Introduce method with keyword parameter name
        builder.IntroduceMethod(
            nameof(MethodWithKeywordParamTemplate),
            buildMethod: m => m.Name = "MethodWithKeywordParam" );

        // Introduce event
        builder.IntroduceEvent( nameof(EventTemplate), buildEvent: e => e.Name = "event" );
    }

    [Template]
    public int FieldTemplate;

    [Template]
    public string PropertyTemplate { get; set; }

    [Template]
    public void MethodTemplate() { }

    [Template]
    public void MethodWithKeywordParamTemplate( string @class ) { }

    [Template]
    public event Action EventTemplate;
}

[IntroduceAllKeywordMembersAspect]
internal class TargetClass
{
}
