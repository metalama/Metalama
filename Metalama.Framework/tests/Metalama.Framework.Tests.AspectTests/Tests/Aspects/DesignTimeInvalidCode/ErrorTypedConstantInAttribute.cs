#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.DesignTimeInvalidCode.ErrorTypedConstantInAttribute;

/*
 * Tests that errorneous expression in aspect attribute does not cause an exception.
 */

internal class Aspect : TypeAspect
{
    private int constructorValue;

    public Aspect(int constructorValue)
    {
        this.constructorValue = constructorValue;
    }

    public Aspect()
    {
    }

    public int PropertyValue { get; set; }

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.IntroduceField("ConstructorValueWas_" + this.constructorValue, typeof(int));
        builder.IntroduceField("PropertyValueWas_" + this.PropertyValue, typeof(int));
    }
}

// <target>
#if TESTRUNNER
[Aspect(int.Parse("42"))]
#endif
internal partial class TargetCode1
{
}

// <target>
#if TESTRUNNER
[Aspect(PropertyValue = int.Parse("42"))]
#endif
internal partial class TargetCode2
{
}