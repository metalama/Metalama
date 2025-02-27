using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.InterfaceMember_ReturnValue;

public class MyAttribute : Attribute;

[AttributeUsage(AttributeTargets.ReturnValue)]
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
    [return: MyAspect] int M(int arg);
}

internal interface IInterfaceB : IInterfaceA { }

// <target>
internal sealed class SomeImplementation : IInterfaceB
{
    public int M(int arg) => arg;
}