using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Partial;

[Inheritable]
public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceAttribute( AttributeConstruction.Create( typeof(SerializableAttribute) ) );
    }
}

// <target>
[MyAspect]
internal partial class C { }

// <target>
internal partial class C { }