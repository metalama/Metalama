// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_OverloadedNoParams;

/*
 * Tests CreateDelegateExpression targeting an overloaded void method with no parameters.
 * Expected: typed delegate expression using Action (non-generic) to disambiguate.
 */

public class DelegateAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override(
            nameof(this.Template),
            new
            {
                target = builder.Target.DeclaringType!.Methods.OfName( "Method" )
                    .Single( m => m.Parameters.Count == 0 )
            } );
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

    public void Method( int x ) { }

    [DelegateAspect]
    public void Invoker() { }
}
