using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Trivia_Between;

public class OldAttribute : Attribute { }

public class NewAttribute : Attribute { }

public class IntroduceAttributeAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var member in builder.Target.Members())
        {
            builder.With( member ).IntroduceAttribute( AttributeConstruction.Create( typeof(NewAttribute) ) );
        }
    }
}

// <target>
[IntroduceAttributeAspect]
internal class IntroduceTarget
{
    // first
    [OldAttribute]

    // second
    private void M() { }
}