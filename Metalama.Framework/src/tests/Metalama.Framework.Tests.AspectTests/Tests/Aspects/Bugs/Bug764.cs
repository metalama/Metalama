// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0618 // WithTypeArguments is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug764;

public class TestData<T>
{
    public T? Value { get; set; }
}

public class MyAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override( nameof(Template), args: new { T = builder.Target.ReturnType } );
    }

    [Template]
    public dynamic? Template<[CompileTime] T>()
    {
        var method = meta.Target.Type.Methods.OfName( "Bar" ).Single();

        // Scenario from the bug: WithTypeArguments (params extension method) with typeof(T).
        method.WithTypeArguments( typeof(T) ).Invoke( new TestData<T>() );

        // Same with MakeGenericInstance (non-obsolete equivalent).
        method.MakeGenericInstance( typeof(T) ).Invoke( new TestData<T>() );

        // WithTypeArguments with multiple params type arguments.
        var method2 = meta.Target.Type.Methods.OfName( "Baz" ).Single();
        method2.WithTypeArguments( typeof(T), typeof(int) ).Invoke( new TestData<T>(), new List<int>() );

        return meta.Proceed();
    }
}

// <target>
public class Target
{
    public void Bar<TValue>( TestData<TValue> data ) { }

    public void Baz<TValue, TExtra>( TestData<TValue> data, List<TExtra> extra ) { }

    [MyAspect]
    public string Foo() => "";
}
