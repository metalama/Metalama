using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Tests.AspectTests.Tests.Options.CrossProject;

[assembly: MyOptions( "FromAssembly" )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Options.CrossProject;

public class PrintOptionsAspect : Attribute, IAspect<IDeclaration>
{
    public void BuildAspect( IAspectBuilder<IDeclaration> builder )
    {
        builder.IntroduceAttribute(
            AttributeConstruction.Create( typeof(ActualOptionsAttribute), new[] { builder.Target.Enhancements().GetOptions<MyOptions>().Value } ) );
    }

    public void BuildEligibility( IEligibilityBuilder<IDeclaration> builder ) { }
}

public class ActualOptionsAttribute : Attribute
{
    public ActualOptionsAttribute( string value ) { }
}

// <target>
[PrintOptionsAspect]
internal class DerivedClass : BaseClass { }

// <target>
[PrintOptionsAspect]
internal class DerivedOfNested : BaseNestingClass.BaseNestedClass { }

// <target>
[MyOptions( "OtherClass" )]
internal class OtherClass
{
    [PrintOptionsAspect]
    internal class C { }
}

// <target>
[PrintOptionsAspect]
internal class ClassWithoutOptions { }

// <target>
[PrintOptionsAspect]
internal class DerivedFromBaseClassWithoutDirectOptions : BaseClassWithoutDirectOptions { }