using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Params;

[AttributeUsage( AttributeTargets.All, AllowMultiple = true )]
public class MyAttribute : Attribute
{
    public MyAttribute( string a, params int[] p ) { }
}

[AttributeUsage( AttributeTargets.All, AllowMultiple = true )]
public class YourAttribute : Attribute
{
    public YourAttribute( string a, params string?[] p ) { }
}

[Inheritable]
public class MyAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var attr1 = AttributeConstruction.Create( typeof(MyAttribute), new object[] { "x", 1, 2, 3, 4, 5 } );
        builder.IntroduceAttribute( attr1 );

        //  Known issue: should pass the 'null' argument, but does not.
        var attr2 = AttributeConstruction.Create( typeof(MyAttribute), new object?[] { "x", null } );
        builder.IntroduceAttribute( attr2, whenExists: OverrideStrategy.New );

        var attr3 = AttributeConstruction.Create( typeof(YourAttribute), new object?[] { "x", null } );
        builder.IntroduceAttribute( attr3, whenExists: OverrideStrategy.New );
    }
}

// <target>
internal class C
{
    [MyAspect]
    private void M() { }
}