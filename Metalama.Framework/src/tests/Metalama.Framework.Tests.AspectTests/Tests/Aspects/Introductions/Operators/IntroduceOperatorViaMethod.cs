// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Operators.IntroduceOperatorViaMethod;

/*
 * Tests introducing an operator via IntroduceMethod with OperatorKind property.
 */

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce a binary operator using IntroduceMethod + OperatorKind.
        builder.IntroduceMethod(
            nameof(BinaryOperatorTemplate),
            buildMethod: m =>
            {
                m.OperatorKind = OperatorKind.Addition;
                // Name and IsStatic are automatically set based on OperatorKind.
                // Override parameter types from template with desired types.
                m.Parameters[0].Type = builder.Target;
                m.Parameters[1].Type = TypeFactory.GetType( typeof(int) );
                m.ReturnType = builder.Target;
            } );

        // Introduce a unary operator using IntroduceMethod + OperatorKind.
        builder.IntroduceMethod(
            nameof(UnaryOperatorTemplate),
            buildMethod: m =>
            {
                m.OperatorKind = OperatorKind.UnaryNegation;
                m.Parameters[0].Type = builder.Target;
                m.ReturnType = builder.Target;
            } );
    }

    [Template]
    public dynamic? BinaryOperatorTemplate( dynamic? left, dynamic? right )
    {
        return meta.Default( meta.Target.Type );
    }

    [Template]
    public dynamic? UnaryOperatorTemplate( dynamic? value )
    {
        return meta.Default( meta.Target.Type );
    }
}

// <target>
[Introduction]
internal class TargetClass
{
    public int Value { get; set; }
}
