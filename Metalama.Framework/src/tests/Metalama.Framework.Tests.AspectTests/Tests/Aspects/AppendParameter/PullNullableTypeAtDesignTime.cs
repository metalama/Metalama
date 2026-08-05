// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

// A pull strategy holds the type of the parameter it introduces as a durable reference, so that the transitive aspect
// carrying it does not pin the compilation it was produced in (issue #1797). Resolving that reference happens while
// the pull runs, which is inside BuildAspect, and resolving a type identifier looks a name up through the namespaces.
// That query is rejected in the BuildAspect context at design time, so the aspect failed with LAMA0041 wrapping
// 'The INamespace.Types API is not supported in the BuildAspect context at design time'.
//
// The equivalent case in Metalama.Extensions is EarlyRequired_DesignTime, which reaches the same code through
// [IntroduceDependency]. This test reproduces it in the Metalama solution and without the dependency injection
// package, so that the defect is covered where the code that has it lives.
//
// The type is nullable because that is the shape the identifier of the failing case had, 'Y:global::System.IFormatProvider?'.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.PullNullableTypeAtDesignTime;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var parameterType = TypeFactory.GetType( typeof(IFormatProvider) ).ToNullableType();

        foreach ( var constructor in builder.Target.Constructors )
        {
            builder.With( constructor )
                .IntroduceParameter(
                    "formatProvider",
                    parameterType,
                    TypedConstant.Default( parameterType ),
                    PullStrategy.IntroduceParameterAndPull(
                        type: parameterType,
                        defaultValue: TypedConstant.Default( parameterType ) ) );
        }
    }
}

// <target>
[MyAspect]
public partial class TargetClass
{
    public TargetClass() { }

    // Chains to the constructor above, so the pull runs and the type of the introduced parameter has to be resolved.
    public TargetClass( int x ) : this() { }
}
