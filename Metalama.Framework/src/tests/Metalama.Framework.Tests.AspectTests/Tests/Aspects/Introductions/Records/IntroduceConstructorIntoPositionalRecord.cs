// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Records.IntroduceConstructorIntoPositionalRecord;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var record = builder.IntroduceRecord(
            "Positional",
            buildRecord: r =>
            {
                r.Accessibility = Accessibility.Public;
                r.AddPositionalParameter( "Value", typeof(int) );
            } );

        // A record that declares a primary constructor accepts another constructor only when that constructor
        // chains to the primary one, which the language requires and the compiler reports as CS8862 otherwise.
        builder.With( record.Declaration )
            .IntroduceConstructor(
                nameof(ConstructorTemplate),
                buildConstructor: c =>
                {
                    c.Accessibility = Accessibility.Public;
                    c.InitializerKind = ConstructorInitializerKind.This;
                    c.AddInitializerArgument( ExpressionFactory.Literal( 0 ) );
                } );
    }

    [Template]
    public void ConstructorTemplate() { }
}

// <target>
[Introduction]
public class TargetType { }
