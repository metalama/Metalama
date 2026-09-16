// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Text;

// Verifies that a synthesized member of a sealed record class and of a record struct is overridden. PrintMembers is
// private on both of those forms and protected virtual on a record class that is not sealed, so the two exercise the
// arm that the unsealed form does not.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.OverrideSynthesizedMember_Sealed;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var sealedClass = builder.IntroduceRecord(
            "SealedClass",
            buildRecord: r =>
            {
                r.Accessibility = Accessibility.Public;
                r.IsSealed = true;
                r.AddPositionalParameter( "Value", typeof(int) );
            } );

        var recordStruct = builder.IntroduceRecord(
            "Struct",
            RecordKind.Struct,
            buildRecord: r =>
            {
                r.Accessibility = Accessibility.Public;
                r.AddPositionalParameter( "Value", typeof(int) );
            } );

        builder.With( sealedClass.Declaration ).IntroduceMethod( nameof(IntroducedPrintMembers), whenExists: OverrideStrategy.Override );
        builder.With( recordStruct.Declaration ).IntroduceMethod( nameof(IntroducedPrintMembers), whenExists: OverrideStrategy.Override );
    }

    [Template( Name = "PrintMembers" )]
    private bool IntroducedPrintMembers( StringBuilder builder )
    {
        var result = meta.Proceed();
        builder.Append( ", Suffix = 1" );

        return result;
    }
}

// <target>
[Introduction]
public class TargetType { }
