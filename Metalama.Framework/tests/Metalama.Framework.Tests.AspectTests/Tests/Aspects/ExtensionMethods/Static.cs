using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.ExtensionMethods.Static;

#pragma warning disable CS0618 // Type or member is obsolete

internal class ReturnNumbers : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var numbers = new object[] { 42 };

        return Enumerable.ToList( numbers );
    }
}

internal class TargetCode
{
    // <target>
    [ReturnNumbers]
    private object Method() => throw new NotImplementedException();
}