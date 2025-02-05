using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Compilation;

[assembly: MyAspect]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Add_Compilation;

public class MyAttribute : Attribute { }

public class MyAspect : CompilationAspect
{
    public override void BuildAspect( IAspectBuilder<ICompilation> builder )
    {
        builder.IntroduceAttribute( AttributeConstruction.Create( typeof(MyAttribute) ) );
    }
}