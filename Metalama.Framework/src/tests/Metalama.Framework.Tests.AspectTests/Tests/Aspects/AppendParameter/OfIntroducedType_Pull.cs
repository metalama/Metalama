// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.OfIntroducedType_Pull;

/// <summary>
/// An aspect that introduces a type and then introduces, into the constructor of its target, a parameter of that
/// introduced type that is pulled from the constructors of the derived types.
/// </summary>
/// <remarks>
/// The pull strategy makes the parameter type durable, and an introduced type has no symbol in the compilation, so a
/// durable reference to it must be a declaration identifier rather than a serializable type identifier.
/// </remarks>
public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var introducedType = builder
            .With( builder.Target.Compilation )
            .WithNamespace( "Introduced" )
            .IntroduceClass( "X", buildType: b => b.Accessibility = Accessibility.Public )
            .Declaration;

        builder.With( builder.Target.Constructors.Single() )
            .IntroduceParameter(
                "p",
                introducedType,
                TypedConstant.Default( introducedType ),
                PullStrategy.IntroduceParameterAndPull( type: introducedType ) );
    }
}

// <target>
[MyAspect]
public class A
{
    public A() { }
}

// <target>
public class B : A
{
    public B() { }
}
