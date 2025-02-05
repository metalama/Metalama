using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.Integration.Tests.Aspects.Bugs.Bug35558;

#pragma warning disable CS0169

public class TestAspect : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        foreach(var field in builder.Target.Fields)
        {
            // First override promoted
            var result = builder.Advice.Override(field, nameof(PropertyTemplate));
            builder.Advice.Override(result.Declaration, nameof(PropertyTemplate));
        }
    }

    [Template]
    public dynamic? PropertyTemplate
    {
        get
        {
            Console.WriteLine("Aspect");
            return meta.Proceed();
        }

        set
        {
            Console.WriteLine("Aspect");
            meta.Proceed();
        }
    }
}

// <target>
[TestAspect]
public partial class TargetClass
{
    private int _field1;
}
