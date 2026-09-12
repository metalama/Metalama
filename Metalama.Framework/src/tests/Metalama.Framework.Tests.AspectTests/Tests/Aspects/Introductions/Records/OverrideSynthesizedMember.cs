// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;
using System.Text;

// Verifies that an aspect overrides the members that the compiler synthesizes for an introduced record, and that
// meta.Proceed() reaches the synthesized implementation of each of them. The body is reproduced from a symbol of the
// intermediate compilation, which holds the declaration that Metalama emits, so the mechanism that serves a record
// read from source serves an introduced one as well once the member is resolved to that symbol. This is the question
// of section 7.2 of Metalama.Framework/docs/introducing-records.md.
//
// The strongly typed Equals is what proves the resolution by signature: the record has two members named Equals that
// take one parameter each, and only the parameter type tells them apart.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.OverrideSynthesizedMember;

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

        var target = builder.With( record.Declaration );

        target.IntroduceMethod( nameof(IntroducedPrintMembers), whenExists: OverrideStrategy.Override );
        target.IntroduceMethod( nameof(IntroducedGetHashCode), whenExists: OverrideStrategy.Override );
        target.IntroduceProperty( nameof(IntroducedEqualityContract), whenExists: OverrideStrategy.Override );

        target.IntroduceMethod(
            nameof(IntroducedEquals),
            whenExists: OverrideStrategy.Override,
            args: new { T = record.Declaration } );

        builder.With( record.Declaration.Methods.OfName( "ToString" ).Single() ).Override( nameof(ToStringTemplate) );

        builder.With( record.Declaration.Methods.OfName( "Deconstruct" ).Single() ).Override( nameof(DeconstructTemplate) );
    }

    [Template( Name = "PrintMembers" )]
    protected bool IntroducedPrintMembers( StringBuilder builder )
    {
        var result = meta.Proceed();
        builder.Append( ", Suffix = 1" );

        return result;
    }

    [Template( Name = "GetHashCode" )]
    public int IntroducedGetHashCode()
    {
        return meta.Proceed() + 1;
    }

    [Template( Name = "EqualityContract" )]
    protected virtual Type IntroducedEqualityContract
    {
        get
        {
            Console.WriteLine( "Reading the equality contract." );

            return meta.Proceed()!;
        }
    }

    [Template( Name = "Equals" )]
    public bool IntroducedEquals<[CompileTime] T>( T? other )
    {
        Console.WriteLine( "Comparing." );

        return meta.Proceed();
    }

    [Template]
    private dynamic? ToStringTemplate()
    {
        Console.WriteLine( "Formatting." );

        return meta.Proceed();
    }

    [Template]
    private dynamic? DeconstructTemplate()
    {
        Console.WriteLine( "Deconstructing." );

        return meta.Proceed();
    }
}

// <target>
[Introduction]
public class TargetType { }
