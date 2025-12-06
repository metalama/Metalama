// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.TypedConstant_NullForgivingOperator;

public class AddParameterWithNullForgiving : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        base.BuildAspect( builder );

        // Non-nullable string parameter with null-forgiving operator on default value.
        // Should generate: string arg1 = default(string)!
        builder.IntroduceParameter(
            "arg1",
            typeof(string),
            TypedConstant.Default( typeof(string), hasNullForgivingOperator: true ) );

        // Non-nullable string parameter without null-forgiving operator.
        // Should generate: string arg2 = default(string)
        builder.IntroduceParameter(
            "arg2",
            typeof(string),
            TypedConstant.Default( typeof(string), hasNullForgivingOperator: false ) );
    }
}

// <target>
internal class TargetCode
{
    [AddParameterWithNullForgiving]
    private TargetCode( int i ) { }
}
