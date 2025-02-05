using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.KeepsTrivia;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(AddMyAspect), typeof(MyAspect))]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.KeepsTrivia;

// This test adds an aspect to the class C and tests that the [target] comment is not removed.

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceAttribute( AttributeConstruction.Create( typeof(SerializableAttribute) ) );
    }
}

public class AddMyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var t in builder.Target.Types)
        {
            builder.With( t ).AddAspect( new MyAspect() );
        }
    }
}

[AddMyAspect]
class P
{
    // <target>
    internal partial class C { }
}