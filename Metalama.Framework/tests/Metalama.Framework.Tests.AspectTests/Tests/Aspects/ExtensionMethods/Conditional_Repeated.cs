// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.ExtensionMethods.Conditional_Repeated;

#pragma warning disable CS0618 // Type or member is obsolete

internal static class MyExtensionMethods
{
    public static List<T> MyToList<T>( this IEnumerable<T> items )
    {
        var list = new List<T>();
        list.AddRange( items );

        return list;
    }
}

internal class ReturnNumbers : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        IEnumerable<object>? numbers = new object[] { 42 };

        foreach (var _ in meta.CompileTime( Enumerable.Range( 1, 2 ) ))
        {
            numbers = numbers.MyToList();
        }

        return numbers;
    }
}

internal class TargetCode
{
    // <target>
    [ReturnNumbers]
    private object Method() => throw new NotImplementedException();
}