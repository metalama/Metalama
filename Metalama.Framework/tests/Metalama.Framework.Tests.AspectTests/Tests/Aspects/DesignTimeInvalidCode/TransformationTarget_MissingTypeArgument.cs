#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.DesignTimeInvalidCode.TransformationTarget_MissingTypeArgument;

/*
 * Tests that transformation target that contains missing type argument does not cause an exception.
 */

public class TestAspect : ConstructorAspect
{
    public override void BuildAspect(IAspectBuilder<IConstructor> builder)
    {
        builder.IntroduceParameter("TestParameter", typeof(int), TypedConstant.Create(1));
    }
}

// <target>
public partial class TargetCode
{
#if TESTRUNNER
    [TestAspect]
    public TargetCode(List<List<>> x, int z, int z2)
    {
    }
#endif
}