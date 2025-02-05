using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Constructors.Parameters_Generic;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceConstructor(
            nameof(Template),
            buildConstructor: introduced => { introduced.AddParameter( "x", builder.Target ); } );

        // TODO: Other members.
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( "This is introduced method." );

        meta.Proceed();
    }
}

// <target>
[Introduction]
internal class TargetClass<T> { }