using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.ExtensionMethods.Conditional;

#pragma warning disable CS0618 // Type or member is obsolete

internal class ReturnNumbers : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var numbers = new object[] { 42 };

        switch (DateTime.Today.DayOfWeek)
        {
            case DayOfWeek.Monday:
                return numbers?.ToReadOnlyList();

            case DayOfWeek.Tuesday:
                return numbers?.ToReadOnlyList().ToReadOnlyList();

            case DayOfWeek.Wednesday:
                return numbers.ToReadOnlyList()?.ToReadOnlyList();

            default:
                return numbers?.ToReadOnlyList()?.ToReadOnlyList();
        }
    }
}

internal class TargetCode
{
    // <target>
    [ReturnNumbers]
    private object Method() => throw new NotImplementedException();
}