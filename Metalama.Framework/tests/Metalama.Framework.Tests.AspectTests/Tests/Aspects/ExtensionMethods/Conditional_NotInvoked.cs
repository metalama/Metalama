using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#if TESTRUNNER
using System.Linq;
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.ExtensionMethods.Conditional_NotInvoked;

#pragma warning disable CS0618 // Type or member is obsolete

internal class ReturnNumbers : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
#if TESTRUNNER
        var numbers = new object[] { 42 };

        return numbers?.ToHashSet();
#else
        return null;
#endif
    }
}

internal class TargetCode
{
    // <target>
    [ReturnNumbers]
    private object Method() => throw new NotImplementedException();
}