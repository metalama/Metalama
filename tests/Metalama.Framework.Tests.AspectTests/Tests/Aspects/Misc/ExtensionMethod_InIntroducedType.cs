using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Diagnostics;

[assembly: Metalama.Framework.Tests.Integration.Tests.Aspects.Misc.ExtensionMethod_InIntroducedType.TheAspect]

namespace Metalama.Framework.Tests.Integration.Tests.Aspects.Misc.ExtensionMethod_InIntroducedType;

class TheAspect : CompilationAspect
{
    [Template]
    static void ExtensionMethodTemplate([This] object self) { }

    [Template]
    static void Usage()
    {
        dynamic o = meta.Cast(TypeFactory.GetType(typeof(object)), new object());

        o.ExtensionMethodTemplate();
    }

    public override void BuildAspect(IAspectBuilder<ICompilation> builder)
    {
        base.BuildAspect(builder);

        var typeAdviser = builder.WithNamespace(typeof(TheAspect).Namespace).IntroduceClass("Target", buildType: typeBuilder => typeBuilder.IsStatic = true);
        typeAdviser.IntroduceMethod(nameof(ExtensionMethodTemplate));
        typeAdviser.IntroduceMethod(nameof(Usage));
    }
}