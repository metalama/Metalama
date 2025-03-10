// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods;

public class MyAspect : TypeAspect
{
    [Introduce]
    public void Introduced()
    {
        meta.Target.Type.Methods.OfName( "Method" ).Single().Invoke();
        meta.Target.Type.MakeGenericInstance( typeof(int) ).Methods.OfName( "Method" ).Single().Invoke();
    }
}

// <target>
[MyAspect]
public class C<T>
{
    public static void Method() { }
}