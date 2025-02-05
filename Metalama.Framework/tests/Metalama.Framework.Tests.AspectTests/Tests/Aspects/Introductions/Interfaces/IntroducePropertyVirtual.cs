#if TEST_OPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
#endif

#if NET6_0_OR_GREATER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroducePropertyVirtual;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var @interface = builder.IntroduceInterface( "ITest");
        var interfaceProperty = @interface.IntroduceProperty( nameof(TestProperty));

        // Implementation type
        var implementation = builder.IntroduceClass("TestImplementation");
        implementation.ImplementInterface( @interface.Declaration);
        var constructor = implementation.IntroduceConstructor( nameof(Constructor));
        implementation.IntroduceProperty( nameof(TestPropertyImplementation), buildProperty: b => { b.Name = "TestProperty"; });

        var usage = builder.IntroduceClass("TestUsage");
        usage.IntroduceMethod( nameof(TestUsageMethod), args: new { T = @interface.Declaration, property = interfaceProperty.Declaration, implementationConstructor = constructor.Declaration });
    }

    [Template]
    public void Constructor()
    {
    }

    [Template]
    public int TestProperty
    {
        get
        {
            Console.WriteLine("Default");
            return 0;
        }

        set
        {
            Console.WriteLine("Default");
        }
    }

    [Template]
    public int TestPropertyImplementation
    {
        get
        {
            Console.WriteLine("Implementation");
            return 0;
        }

        set
        {
            Console.WriteLine("Implementation");
        }
    }

    [Template]
    public T TestUsageMethod<[CompileTime] T>(T instance, [CompileTime] IProperty property, [CompileTime] IConstructor implementationConstructor)
    {
        property.With(instance).Value = property.With(instance).Value + 1;
        return implementationConstructor.Invoke();
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
#endif