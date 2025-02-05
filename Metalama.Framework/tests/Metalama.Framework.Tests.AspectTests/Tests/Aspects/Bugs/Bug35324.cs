using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Fabrics;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug35324;

public class TestAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.IntroduceMethod(nameof(Foo), args: new { T = typeof(int) });
    }

    [Template]
    public void Foo<[CompileTime] T>(int x = 0)
    {
        Console.WriteLine($"{typeof(T)}");
    }
}

// <target>
[Test]
public class TestClass
{
}