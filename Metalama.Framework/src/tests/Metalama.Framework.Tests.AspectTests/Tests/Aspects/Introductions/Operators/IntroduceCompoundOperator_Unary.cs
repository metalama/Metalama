// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER && NET8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Operators.IntroduceCompoundOperator_Unary;

/*
 * Tests introducing a C# 14 unary compound assignment operator (++) via IntroduceMethod.
 * Unary assignment operators are instance methods with 0 parameters.
 */

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce a unary compound assignment operator (++).
        // These are instance methods (not static) with 0 parameters.
        builder.IntroduceMethod(
            nameof(IncrementAssignmentTemplate),
            buildMethod: m =>
            {
                m.OperatorKind = OperatorKind.IncrementAssignment;
                // OperatorKind auto-sets IsStatic to false for compound operators.
                // Unary assignment operators have 0 parameters (just operates on this).
            } );
    }

    [Template]
    public void IncrementAssignmentTemplate()
    {
        // In a real implementation, this would increment the instance.
        // For the test, we just verify the operator is introduced correctly.
    }
}

// <target>
[Introduction]
internal class TargetClass
{
    public int Value { get; set; }
}

#endif
