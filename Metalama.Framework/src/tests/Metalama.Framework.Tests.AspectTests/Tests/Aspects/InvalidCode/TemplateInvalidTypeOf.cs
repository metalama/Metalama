// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Concurrent;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.InvalidCode.TemplateInvalidTypeOf;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var x1 = StaticCode.Foo(typeof(Object));
        var x2 = StaticCode.Foo(typeof(System.Object));
        var x3 = StaticCode.Foo(typeof(global::System.Object));
        var x4 = StaticCode.Foo(typeof(ConcurrentQueue<int>));
        var x5 = StaticCode.Foo(typeof(System.Collections.Concurrent.ConcurrentQueue<int>));
        var x6 = StaticCode.Foo(typeof(global::System.Collections.Concurrent.ConcurrentQueue<int>));

        return meta.Proceed();
    }

    [Template]
    private int OtherTemplate([CompileTime] Type type)
    {
        return type.Name.Length;
    }
}

internal class StaticCode
{
    public static int Foo(Type type) => type.FullName.Length;
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private int Method(int a)
    {
        return a;
    }
}