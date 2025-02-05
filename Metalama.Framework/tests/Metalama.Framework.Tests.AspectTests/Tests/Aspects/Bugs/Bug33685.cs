using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33685;

public class TestAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override( nameof(Template) );
    }

    [Template]
    public dynamic? Template<T>()
    {
        var method = meta.Target.Type.Methods.OfName( "Bar" ).Single();
        method.Invoke( new TestData<T>() );

        return meta.Proceed();
    }
}

public class TestAspect2 : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override(
            nameof(Template),
            args: new { T = typeof(int) } );
    }

    [Template]
    public dynamic? Template<[CompileTime] T>()
    {
        new TestData<T>();

        return meta.Proceed();
    }
}

[RunTimeOrCompileTime]
public class TestData<T> { }

// <target>
internal class Target
{
    [TestAspect]
    public void Foo<T>() { }

    [TestAspect2]
    public void Foo2<T>() { }

    public void Bar<T>( TestData<T> data ) { }
}