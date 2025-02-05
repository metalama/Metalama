using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using System.ComponentModel;

#if TEST_OPTIONS
// @DependencyDefinedConstant(DEPENDENCY)
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Options.GetDependencyOptions_CrossProject;

internal class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var options = builder.Target.Enhancements().GetOptions<Options>();

        builder.IntroduceAttribute( AttributeConstruction.Create( typeof(DescriptionAttribute), new[] { options.ProjectPath } ) );
    }
}

// <target>
internal class Outer
{
    [Aspect]
    private class Target : C { }
}