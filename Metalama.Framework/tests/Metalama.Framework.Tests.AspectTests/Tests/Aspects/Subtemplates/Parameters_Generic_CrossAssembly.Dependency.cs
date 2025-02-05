using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Parameters_Generic_CrossAssembly;

public class BaseAspect : TypeAspect
{
    [Template]
    protected void NonOptional<[CompileTime] T>(int a, [CompileTime] int b)
    {
        Console.WriteLine($"called template T={typeof(T)} a={a} b={b}");
    }

    [Template]
    protected void Optional<[CompileTime] T>(int a = 0, [CompileTime] int b = 0)
    {
        Console.WriteLine($"called template T={typeof(T)} a={a} b={b}");
    }

    [Template]
    protected void NonOptional2<[CompileTime] T>([CompileTime] int a)
    {
        Console.WriteLine($"called template T={typeof(T)} a={a}");
    }

    [Template]
    protected void Optional2<[CompileTime] T>([CompileTime] int a = 0, [CompileTime] int b = 0)
    {
        Console.WriteLine($"called template T={typeof(T)} a={a} b={b}");
    }
}