using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceEventVirtual;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var @interface = builder.Advice.IntroduceInterface(builder.Target, "ITest");
        var interfaceProperty = builder.Advice.IntroduceProperty(@interface.Declaration, nameof(TestProperty));

        // Implementation type
        var implementation = builder.Advice.IntroduceClass(builder.Target, "TestImplementation");
        builder.Advice.ImplementInterface(implementation.Declaration, @interface.Declaration);
        var constructor = builder.Advice.IntroduceConstructor(implementation.Declaration, nameof(Constructor));
        builder.Advice.IntroduceProperty(implementation.Declaration, nameof(TestPropertyImplementation), buildProperty: b => { b.Name = "TestProperty"; });

        var usage = builder.Advice.IntroduceClass(builder.Target, "TestUsage");
        builder.Advice.IntroduceMethod(usage.Declaration, nameof(TestUsageMethod), args: new { T = @interface.Declaration, property = interfaceProperty.Declaration, implConstructor = constructor.Declaration });
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
    public T TestUsageMethod<[CompileTime] T>(T instance, [CompileTime] IProperty property, [CompileTime] IConstructor implConstructor)
    {
        property.With(instance).Value = property.With(instance).Value + 1;
        return implConstructor.Invoke();
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }