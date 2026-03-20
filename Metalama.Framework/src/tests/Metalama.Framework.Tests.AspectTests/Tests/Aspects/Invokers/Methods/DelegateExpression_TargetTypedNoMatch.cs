// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_TargetTypedNoMatch;

/*
 * Tests CreateDelegateExpression when the target delegate type does NOT match the method signature.
 * The expression should fall back to Action<int> disambiguation instead of using the target type.
 */

public class DelegateAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var targetInt = builder.Target.DeclaringType!.Methods.OfName( "Method" )
            .Single( m => m.Parameters.Count == 1 && m.Parameters[0].Type.SpecialType == SpecialType.Int32 );

        builder.Override(
            nameof(this.Template),
            new { target = targetInt } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IMethod target )
    {
        // The delegate expression is passed to a method expecting Delegate (base type, not a matching delegate).
        // Since Delegate is not a specific delegate type, the expression should fall back to Action<int>.
        TargetClass.AcceptDelegate( target.CreateDelegateExpression().Value );

        return meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    public void Method( int x ) { }

    public void Method( string s ) { }

    public static void AcceptDelegate( Delegate d ) { }

    [DelegateAspect]
    public void Invoker() { }
}
