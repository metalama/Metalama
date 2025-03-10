// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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