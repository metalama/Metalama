// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_TargetTyped;

/*
 * Tests CreateDelegateExpression (without an explicit delegate type) when the delegate expression
 * is passed to a method whose parameter type (Action<int>) exactly matches the method signature.
 * Because the target type flows through GetArguments, the expression should be simplified to a
 * bare method group.
 */

public class DelegateAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var targetInt = builder.Target.DeclaringType!.Methods.OfName( "Method" )
            .Single( m => m.Parameters.Count == 1 && m.Parameters[0].Type.SpecialType == SpecialType.Int32 );

        var acceptAction = builder.Target.DeclaringType!.Methods.OfName( "AcceptAction" ).Single();

        builder.Override(
            nameof(this.Template),
            new { target = targetInt, acceptAction } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IMethod target, [CompileTime] IMethod acceptAction )
    {
        // The delegate expression is passed as an IExpression argument to a method expecting Action<int>.
        // The target type flows through GetArguments, enabling exact-match simplification to a bare method group.
        acceptAction.Invoke( target.CreateDelegateExpression() );

        return meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    public void Method( int x ) { }

    public void Method( string s ) { }

    public static void AcceptAction( Action<int> action ) { }

    [DelegateAspect]
    public void Invoker() { }
}
