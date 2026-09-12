// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.IntroduceMethod;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Member introduction is valid on a record, which section 4 of
        // Metalama.Framework/docs/introducing-records.md states. The declaration of a record that declares a
        // positional parameter list has no member list until a member is added to it.
        var record = builder.IntroduceRecord(
            "Positional",
            buildRecord: r =>
            {
                r.Accessibility = Accessibility.Public;
                r.AddPositionalParameter( "Value", typeof(int) );
            } );

        var target = builder.With( record.Declaration );

        target.IntroduceMethod( nameof(MethodTemplate) );
        target.IntroduceAutomaticProperty( "Label", typeof(string) );
        target.IntroduceField( "_counter", typeof(int) );
    }

    [Template]
    public int MethodTemplate() => 42;
}

// <target>
[Introduction]
public class TargetType { }
