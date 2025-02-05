using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.ExtensionMethods.Conditional_CompileTime;

#pragma warning disable CS0618 // Type or member is obsolete

[CompileTime]
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
        var numbers = meta.CompileTime( new int[] { 42 } );

        switch (DateTime.Today.DayOfWeek)
        {
            case DayOfWeek.Monday:
                return numbers?.MyToList();

            case DayOfWeek.Tuesday:
                return numbers?.MyToList().MyToList();

            case DayOfWeek.Wednesday:
                return numbers.MyToList()?.MyToList();

            default:
                return numbers?.MyToList()?.MyToList();
        }
    }
}

internal class TargetCode
{
    // <target>
    [ReturnNumbers]
    private object Method() => throw new NotImplementedException();
}