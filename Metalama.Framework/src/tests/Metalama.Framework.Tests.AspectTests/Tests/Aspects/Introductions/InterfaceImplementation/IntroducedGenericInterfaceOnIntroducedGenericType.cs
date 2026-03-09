// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @IncludeAllSeverities
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IntroducedGenericInterfaceOnIntroducedGenericType;

public class IntroductionAttribute : TypeAspect
{
    private static readonly DiagnosticDefinition<string> _info = new( "MY001", Severity.Warning, "{0}" );

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
        testImplementation.ImplementInterface(
            testInterface.Declaration.MakeGenericInstance( testImplementation.Declaration.TypeParameters[0] ) );

        // Verify AllImplementedInterfaces and ImplementedInterfaces on the definition Test<U>.
        var definition = testImplementation.Declaration;

        foreach ( var iface in definition.AllImplementedInterfaces )
        {
            builder.Diagnostics.Report( _info.WithArguments( $"Test<U>.AllImplementedInterfaces: {iface.Name}<{iface.TypeArguments[0]}>" ) );
        }

        foreach ( var iface in definition.ImplementedInterfaces )
        {
            builder.Diagnostics.Report( _info.WithArguments( $"Test<U>.ImplementedInterfaces: {iface.Name}<{iface.TypeArguments[0]}>" ) );
        }

        // Create a bound type Test<int> and verify that AllImplementedInterfaces and ImplementedInterfaces
        // return bound interface types (ITest<int>), not the generic type instance (ITest<T>).
        var intType = builder.Target.Compilation.Factory.GetSpecialType( SpecialType.Int32 );
        var boundType = definition.MakeGenericInstance( intType );

        foreach ( var iface in boundType.AllImplementedInterfaces )
        {
            builder.Diagnostics.Report( _info.WithArguments( $"Test<int>.AllImplementedInterfaces: {iface.Name}<{iface.TypeArguments[0]}>" ) );
        }

        foreach ( var iface in boundType.ImplementedInterfaces )
        {
            builder.Diagnostics.Report( _info.WithArguments( $"Test<int>.ImplementedInterfaces: {iface.Name}<{iface.TypeArguments[0]}>" ) );
        }
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
