// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.ExtensionMethods.NoReceiver;

#pragma warning disable CS0618 // Type or member is obsolete

[RunTimeOrCompileTime]
internal static class Outer
{
    internal static List<T> MyToList<T>( this IEnumerable<T> source ) => source.ToList();

    internal class ReturnNumbers : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var numbers = new object[] { 42 };

            return numbers.MyToList();
        }
    }
}

internal class TargetCode
{
    // <target>
    [Outer.ReturnNumbers]
    private object Method() => throw new NotImplementedException();
}