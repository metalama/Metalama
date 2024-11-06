using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Diagnostics;

namespace Metalama.Framework.Tests.Integration.Tests.Aspects.Misc.ExtensionMethods;

class TheAspect : TypeAspect
{
    [Introduce]
    static void ExtensionMethodTemplate([This] object self) { }

    [Template]
    static void RegularTemplate(in int o) { }

    [Introduce]
    static void Usage()
    {
        dynamic o = meta.Cast(TypeFactory.GetType(typeof(object)), new object());

        o.ExtensionMethodTemplate();

        dynamic i = meta.Cast(TypeFactory.GetType(typeof(int)), 42);

        i.RegularTemplate();
    }

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        base.BuildAspect(builder);

        builder.IntroduceMethod(nameof(RegularTemplate), buildMethod: methodBuilder => methodBuilder.Parameters[0].IsThis = true);
    }
}

// <target>
[TheAspect]
static class Target
{
}