#if TEST_OPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
#endif

#if NET6_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodVirtual;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var @interface = builder.IntroduceInterface( "ITest");
        var interfaceMethod = @interface.IntroduceMethod( nameof(TestMethod) );

        // Implementation type
        var implementation = builder.IntroduceClass("TestImplementation");
        implementation.ImplementInterface( @interface.Declaration);
        var constructor = implementation.IntroduceConstructor( nameof(Constructor));
        implementation.IntroduceMethod( nameof(TestMethodImplementation), buildMethod: b => { b.Name = "TestMethod"; });

        var usage = builder.IntroduceClass("TestUsage");
        usage.IntroduceMethod( nameof(TestUsageMethod), args: new { T = @interface.Declaration, method = interfaceMethod.Declaration, implementationConstructor = constructor.Declaration });
    }

    [Template]
    public void Constructor()
    {
    }

    [Template]
    public void TestMethod()
    {
        Console.WriteLine("Default");
    }

    [Template]
    public void TestMethodImplementation()
    {
        Console.WriteLine("Implementation");
    }

    [Template]
    public T TestUsageMethod<[CompileTime] T>(T instance, [CompileTime] IMethod method, [CompileTime] IConstructor implementationConstructor)
    {
        method.With(instance).Invoke();
        return implementationConstructor.Invoke();
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
#endif