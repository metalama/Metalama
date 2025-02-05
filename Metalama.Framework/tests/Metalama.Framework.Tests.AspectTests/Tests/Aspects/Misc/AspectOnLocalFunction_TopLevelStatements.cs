#if TEST_OPTIONS
// @Include(Include\__AspectOnLocalFunction_TopLevelStatements.cs)
// @OutputAssemblyType(Exe)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.AspectOnLocalFunction_TopLevelStatements;

internal class MethodAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Hello, world." );

        return meta.Proceed();
    }
}

internal class MethodBaseAspect : Attribute, IAspect<IMethodBase>
{
    public void BuildAspect( IAspectBuilder<IMethodBase> builder ) { }

    public void BuildEligibility( IEligibilityBuilder<IMethodBase> builder ) { }
}

internal class Contract : ContractAspect
{
    public override void Validate( dynamic? value ) { }
}