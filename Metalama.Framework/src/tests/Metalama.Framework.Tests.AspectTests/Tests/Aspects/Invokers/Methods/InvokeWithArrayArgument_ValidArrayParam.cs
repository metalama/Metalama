// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.InvokeWithArrayArgument_ValidArrayParam;

/*
 * Tests that invoking a method that actually expects an array parameter
 * still works correctly when passing an array argument. See issue #784.
 */

public class InvokerAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override(
            nameof(this.Template),
            new { target = builder.Target.DeclaringType!.Methods.OfName( "Method" ).Single() } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IMethod target )
    {
        target.Invoke( new int[] { 1, 2, 3 } );

        return meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    public void Method( int[] values ) { }

    [InvokerAspect]
    public void Invoker() { }
}
