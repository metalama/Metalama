// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IntroducedGenericInterfaceOnIntroducedGenericType;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce interface ITest<T>.
        var testInterface = builder.IntroduceInterface(
            "ITest",
            buildType: b =>
            {
                b.AddTypeParameter( "T" );
            } );

        // Introduce class Test<U>.
        var testImplementation = builder.IntroduceClass(
            "Test",
            buildType: b =>
            {
                b.AddTypeParameter( "U" );
            } );

        // Make class Test<U> : ITest<U>.
        // This fails with an assertion when populating AllImplementedInterfaces for ITest<U> (which is a type instance, not the definition).
        testImplementation.ImplementInterface(
            testInterface.Declaration.MakeGenericInstance( testImplementation.Declaration.TypeParameters[0] ) );
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
