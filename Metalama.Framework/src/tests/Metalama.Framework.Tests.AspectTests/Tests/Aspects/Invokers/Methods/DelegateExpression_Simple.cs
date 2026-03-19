// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_Simple;

/*
 * Tests CreateDelegateExpression targeting a non-overloaded void method.
 * Expected: simple method group expression (this.Method).
 */

public class DelegateAspect : MethodAspect
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
        var action = target.CreateDelegateExpression().Value;

        return meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    public void Method() { }

    [DelegateAspect]
    public void Invoker() { }
}
