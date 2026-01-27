// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Operators.IntroduceOperatorViaMethod_CannotChangeIsStatic;

/*
 * Tests that setting IsStatic after OperatorKind is set throws an exception.
 */

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod(
            nameof(OperatorTemplate),
            buildMethod: m =>
            {
                m.OperatorKind = OperatorKind.Addition;
                // This should throw - IsStatic cannot be changed after OperatorKind is set.
                m.IsStatic = false;
            } );
    }

    [Template]
    public dynamic? OperatorTemplate( dynamic? left, dynamic? right )
    {
        return meta.Default( meta.Target.Type );
    }
}

// <target>
[Introduction]
internal class TargetClass { }
