using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug35087;

public class TestAspect : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var @namespace = builder.Advice.WithNamespace(builder.Target.ContainingNamespace, "TestNamespace");
        var type = @namespace.IntroduceClass("TestType");
        type.ImplementInterface(typeof(ITestType));
    }

    [InterfaceMember]
    public void Foo()
    {
    }
}

public interface ITestType
{
    void Foo();
}

// <target>
[TestAspect]
public class Target
{
}