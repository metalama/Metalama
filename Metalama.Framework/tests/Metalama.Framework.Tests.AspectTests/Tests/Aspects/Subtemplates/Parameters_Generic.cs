using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Parameters_Generic;

internal class Aspect : TypeAspect
{
    [Introduce]
    private int Add(int a)
    {
        NonOptional<int>(a, meta.CompileTime(1));
        Optional<int>();
        Optional<int>(a: a);
        Optional<int>(b: meta.CompileTime(1));
        Optional<int>(a: a, b: meta.CompileTime(1));

        meta.InvokeTemplate(nameof(NonOptional2), args: new { T = typeof(int), a = 1 });
        meta.InvokeTemplate(nameof(Optional2), args: new { T = typeof(int) });
        meta.InvokeTemplate(nameof(Optional2), args: new { T = typeof(int), a = 1 });
        meta.InvokeTemplate(nameof(Optional2), args: new { T = typeof(int), b = 1 });
        meta.InvokeTemplate(nameof(Optional2), args: new { T = typeof(int), a = 1, b = 1 });

        throw new Exception();
    }

    [Template]
    private void NonOptional<[CompileTime] T>(int a, [CompileTime] int b)
    {
        Console.WriteLine($"called template T={typeof(T)} a={a} b={b}");
    }

    [Template]
    private void Optional<[CompileTime] T>(int a = 0, [CompileTime] int b = 0)
    {
        Console.WriteLine($"called template T={typeof(T)} a={a} b={b}");
    }

    [Template]
    private void NonOptional2<[CompileTime] T>([CompileTime] int a)
    {
        Console.WriteLine($"called template T={typeof(T)} a={a}");
    }

    [Template]
    private void Optional2<[CompileTime] T>([CompileTime] int a = 0, [CompileTime] int b = 0)
    {
        Console.WriteLine($"called template T={typeof(T)} a={a} b={b}");
    }
}

// <target>
[Aspect]
internal class TargetCode { }