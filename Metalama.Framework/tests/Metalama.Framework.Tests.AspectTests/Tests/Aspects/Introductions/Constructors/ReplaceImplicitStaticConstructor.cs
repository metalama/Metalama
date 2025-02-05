using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Constructors.ReplaceImplicitDefaultConstructor;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceConstructor(
            nameof(Template),
            buildConstructor: c => { c.IsStatic = true; } );
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( "Before" );
        meta.Proceed();
        Console.WriteLine( "After" );
    }
}

// <target>
[Introduction]
internal class TargetClass
{
    public static int F = 42;

    public static int P { get; set; } = 42;
}