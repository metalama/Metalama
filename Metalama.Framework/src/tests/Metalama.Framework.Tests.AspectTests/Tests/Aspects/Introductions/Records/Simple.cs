// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.Simple;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceRecord(
            "Positional",
            buildRecord: r =>
            {
                r.Accessibility = Accessibility.Public;
                r.AddPositionalParameter( "Name", typeof(string) );
                r.AddPositionalParameter( "Count", typeof(int) );
            } );

        // A record that declares no positional parameter emits no parameter list, and the compiler synthesizes no
        // Deconstruct method for it.
        builder.IntroduceRecord( "NonPositional", buildRecord: r => r.Accessibility = Accessibility.Public );

        builder.IntroduceRecord(
            "PositionalStruct",
            RecordKind.Struct,
            buildRecord: r =>
            {
                r.Accessibility = Accessibility.Public;
                r.AddPositionalParameter( "X", typeof(double) );
                r.AddPositionalParameter( "Y", typeof(double) );
            } );

        builder.IntroduceRecord(
            "ReadOnlyStruct",
            RecordKind.Struct,
            buildRecord: r =>
            {
                r.Accessibility = Accessibility.Public;
                r.IsReadOnly = true;
                r.AddPositionalParameter( "Value", typeof(int) );
            } );

        // The members that the compiler synthesizes from a record declaration are registered in the code model and
        // are emitted by nothing, so none of them appears in the output below. Section 4.2 of
        // Metalama.Framework/docs/future/introducing-types.md states the rule, and the unit tests assert that they
        // are nevertheless present in the code model.
    }
}

// <target>
[Introduction]
public class TargetType { }
