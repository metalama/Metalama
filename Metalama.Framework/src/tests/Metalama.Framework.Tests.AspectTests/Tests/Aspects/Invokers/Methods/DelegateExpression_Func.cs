// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_Func;

/*
 * Tests CreateDelegateExpression targeting an overloaded method with a return value.
 * Expected: typed delegate expression using Func<int, string> to disambiguate.
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
                    .Single( m => m.Parameters.Count == 1 && m.ReturnType.SpecialType == SpecialType.String )
            } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IMethod target )
    {
        var delegateExpr = target.CreateDelegateExpression();
        Func<int, string> func = delegateExpr.Value;

        return meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    public void Method( int x ) { }

    public string Method( int x, string prefix = "" ) { return prefix + x; }

    [DelegateAspect]
    public void Invoker() { }
}
