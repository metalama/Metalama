#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER
using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS9113 // Parameter is unread.

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Constructors.Existing_PrimaryClass_DifferentSignature;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceConstructor(
            nameof(Template),
            buildConstructor: c =>
            {
                c.InitializerKind = ConstructorInitializerKind.This;
                c.AddInitializerArgument( TypedConstant.Create( 42 ) );
            } );
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( "This is introduced constructor." );
    }
}

// <target>
[Introduction]
internal class TargetClass( int x ) { }
#endif