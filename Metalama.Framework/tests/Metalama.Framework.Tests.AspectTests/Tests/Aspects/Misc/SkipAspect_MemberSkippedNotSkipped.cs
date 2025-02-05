using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.SkipAspect_MemberSkippedNotSkipped;

public interface IMyInterface
{
    void Foo();
}

public class TestAttribute : MethodAspect
{
    [Introduce(WhenExists = OverrideStrategy.Ignore)]
    private readonly IMyInterface _testDependency;

    public override void BuildAspect(IAspectBuilder<IMethod> builder)
    {
        if (!builder.Target.Attributes.OfAttributeType(typeof(DisableAspectAttribute)).Any())
        {
            builder.Advice.Override(builder.Target, nameof(this.OverrideMethod) );
        }
        else
        {
            builder.SkipAspect();
        }
    }

    [Template]
    public dynamic? OverrideMethod()
    {
        foreach(var field in meta.Target.Type.Fields)
        {
            Console.WriteLine(field.Name);
        }

        this._testDependency.Foo();
        return meta.Proceed();
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
public sealed class DisableAspectAttribute : Attribute
{
}

// <target>
public class TargetClass
{
    [Test]
    [DisableAspect]
    public DateTime Method1() 
    {
        return default;
    }

    [Test]
    public double Method2()
    {
        return default;
    }
}