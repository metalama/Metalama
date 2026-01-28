// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.NullConditionalAssignment_Template;

// Compile-time helper class
[CompileTime]
internal class CompileTimeNode
{
    public CompileTimeNode? Next { get; set; }
    public int Value { get; set; }
}

internal class TheAspect : OverrideMethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        // Create compile-time node and store in tag
        var node = new CompileTimeNode { Value = 10 };

        builder.Override( nameof(OverrideMethod), tags: new { Node = node } );
    }

    public override dynamic? OverrideMethod()
    {
        // Get compile-time node from tag
        var node = (CompileTimeNode?)meta.Tags["Node"];

        // Compile-time null-conditional assignments
        node?.Value = 42;
        node?.Next?.Value = 100;

        // The compile-time value is used to generate run-time code
        var computedValue = node?.Value ?? 0;

        return computedValue;
    }
}

// <target>
internal class C
{
    [TheAspect]
    public int M()
    {
        return 0;
    }
}

#endif
