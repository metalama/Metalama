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