using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.InterfaceMember_Parameter;

public class MyAttribute : Attribute;

[Inheritable]
public class MyAspect : ParameterAspect
{
    public override void BuildAspect(IAspectBuilder<IParameter> builder)
    {
        builder.IntroduceAttribute(AttributeConstruction.Create(typeof(MyAttribute)));
    }
}

internal interface IInterfaceA
{
    void M([MyAspect] int arg);
}

internal interface IInterfaceB : IInterfaceA { }

// <target>
internal sealed class SomeImplementation : IInterfaceB
{
    public void M(int arg)
    {
        throw new NotImplementedException();
    }
}