using System;
using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.ExtensionMethods.Simple_NotInvoked;

#pragma warning disable CS0618 // Type or member is obsolete

internal class ReturnNumbers : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var numbers = new object[] { 42 };

        Func<IReadOnlyList<object>> toList = numbers.ToReadOnlyList;

        return toList;
    }
}

internal class TargetCode
{
    // <target>
    [ReturnNumbers]
    private object Method() => throw new NotImplementedException();
}