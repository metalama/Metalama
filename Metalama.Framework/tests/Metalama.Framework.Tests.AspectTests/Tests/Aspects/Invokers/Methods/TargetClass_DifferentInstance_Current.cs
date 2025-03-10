// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.TargetClass_DifferentInstance_Current;

/*
 * Tests current invoker targeting a method from a different instance.
 */

public class InvokerAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var anotherMethod = builder.Target.DeclaringType!.Methods.OfName( "Method" ).Single();

        builder.With( anotherMethod ).Override( nameof(AnotherMethodTemplate) );

        builder.Override(
            nameof(Template),
            new { target = anotherMethod } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IMethod target )
    {
        target.With( (IExpression)meta.Target.Method.Parameters[0].Value!, InvokerOptions.Current ).Invoke();

        return meta.Proceed();
    }

    [Template]
    public void AnotherMethodTemplate()
    {
        Console.WriteLine();
    }
}

// <target>
public class TargetClass
{
    public void Method() { }

    [InvokerAspect]
    public void Invoker( TargetClass instance ) { }
}