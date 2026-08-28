// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.OfIntroducedNullableType_Pull;

/// <summary>
/// An aspect that introduces a type and then introduces, into the constructor of its target, a parameter whose type is
/// the nullable form of that introduced type, pulled from the constructors of the derived types.
/// </summary>
/// <remarks>
/// This is the shape of the dependency that <c>IntroduceDependency</c> builds when it is not required, and of the
/// <c>ImplementMetricsAspect</c> of the metrics sample. The parameter has to be declared nullable, because its default
/// value is <c>default</c>: a parameter declared non-nullable and defaulted to <c>default</c> is reported as CS8625.
/// The nullability is therefore carried by the identifier that the pull strategy makes of the type, the type being
/// introduced rather than read from source. See issue #1840.
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

        var nullableType = introducedType.ToNullable();

        builder.With( builder.Target.Constructors.Single() )
            .IntroduceParameter(
                "p",
                nullableType,
                TypedConstant.Default( nullableType ),
                PullStrategy.IntroduceParameterAndPull( type: nullableType ) );
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
