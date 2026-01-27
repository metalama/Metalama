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

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Operators.IntroduceCompoundOperator_Binary;

/*
 * Tests introducing a C# 14 binary compound assignment operator (+=) via IntroduceMethod.
 * Binary assignment operators are instance methods with 1 parameter.
 */

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce a binary compound assignment operator (+=).
        // These are instance methods (not static) with 1 parameter.
        builder.IntroduceMethod(
            nameof(AdditionAssignmentTemplate),
            buildMethod: m =>
            {
                m.OperatorKind = OperatorKind.AdditionAssignment;
                // OperatorKind auto-sets IsStatic to false for compound operators.
                // Binary assignment operators have 1 parameter (the right operand).
                m.Parameters[0].Type = TypeFactory.GetType( typeof(int) );
            } );
    }

    [Template]
    public void AdditionAssignmentTemplate( dynamic? value )
    {
        // In a real implementation, this would modify the instance.
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
