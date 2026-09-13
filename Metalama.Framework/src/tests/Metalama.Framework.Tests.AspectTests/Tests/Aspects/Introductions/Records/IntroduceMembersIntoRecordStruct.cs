// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.IntroduceMembersIntoRecordStruct;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introducing a field into a record struct replaces the implicit parameterless constructor of that
        // record struct, which is the same path that an ordinary struct takes. That constructor is the member that
        // section 4.2 of Metalama.Framework/docs/introducing-types.md registers and that nothing emits.
        var record = builder.IntroduceRecord(
            "PositionalStruct",
            RecordKind.Struct,
            buildRecord: r =>
            {
                r.Accessibility = Accessibility.Public;
                r.AddPositionalParameter( "Value", typeof(int) );
            } );

        var target = builder.With( record.Declaration );

        target.IntroduceField( "_counter", typeof(int) );
        target.IntroduceMethod( nameof(MethodTemplate) );
    }

    [Template]
    public int MethodTemplate() => 42;
}

// <target>
[Introduction]
public class TargetType { }
