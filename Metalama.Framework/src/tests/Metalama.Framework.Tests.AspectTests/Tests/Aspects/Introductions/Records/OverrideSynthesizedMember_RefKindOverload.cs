// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

// Verifies that the reference kind of a parameter takes part in the resolution of a member that the compiler
// synthesizes. The aspect introduces a method named Deconstruct whose parameters have the types of the positional
// parameters and are passed by value, beside the Deconstruct method that the compiler synthesizes, whose parameters
// have those types and are out parameters. The two overloads differ by the reference kind alone.
//
// LinkerInjectionRegistry resolves a synthesized member by name and by signature on the symbol of the declaring type.
// A comparison that ignored the reference kind would accept either overload, and which one it reached would depend on
// the order in which the members of the type are enumerated. This test passes with that comparison and without it,
// because the enumeration happens to reach the synthesized method first. It is written to cover the pair of
// overloads, not to prove the comparison: the order is not something the test can decide.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.OverrideSynthesizedMember_RefKindOverload;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var record = builder.IntroduceRecord(
            "Positional",
            RecordKind.Class,
            buildRecord: r =>
            {
                r.Accessibility = Accessibility.Public;
                r.AddPositionalParameter( "Name", typeof(string) );
                r.AddPositionalParameter( "Count", typeof(int) );
            } );

        builder.With( record.Declaration ).IntroduceMethod( nameof(ByValueDeconstruct), buildMethod: m => m.Name = "Deconstruct" );

        var synthesizedDeconstruct =
            record.Declaration.Methods.OfName( "Deconstruct" ).Single( m => m.Parameters.All( p => p.RefKind == RefKind.Out ) );

        builder.With( synthesizedDeconstruct ).Override( nameof(DeconstructTemplate) );
    }

    [Template]
    public void ByValueDeconstruct( string name, int count )
    {
        Console.WriteLine( $"This is the overload that takes its parameters by value: {name}, {count}." );
    }

    [Template]
    private dynamic? DeconstructTemplate()
    {
        Console.WriteLine( "This is the override of the synthesized method." );

        return meta.Proceed();
    }
}

// <target>
[Introduction]
public class TargetType { }
