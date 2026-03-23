// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_TargetTypedCustomDelegate;

/*
 * Tests CreateDelegateExpression with a custom delegate type, when the delegate expression is passed
 * to a method whose parameter type exactly matches the method's signature. The expression should be
 * simplified to a bare method group because the target type enables overload resolution.
 */

public delegate void MyIntAction( int x );

public class DelegateAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var targetInt = builder.Target.DeclaringType!.Methods.OfName( "Method" )
            .Single( m => m.Parameters.Count == 1 && m.Parameters[0].Type.SpecialType == SpecialType.Int32 );

        var delegateType = (INamedType) builder.Target.Compilation.Factory.GetTypeByReflectionType( typeof(MyIntAction) );

        var acceptDelegate = builder.Target.DeclaringType!.Methods.OfName( "AcceptDelegate" ).Single();

        builder.Override(
            nameof(this.Template),
            new { target = targetInt, delegateType, acceptDelegate } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IMethod target, [CompileTime] INamedType delegateType, [CompileTime] IMethod acceptDelegate )
    {
        // The delegate expression with explicit delegate type is passed to a method expecting MyIntAction.
        // Since MyIntAction is an exact match, the expression simplifies to a bare method group.
        acceptDelegate.Invoke( target.CreateDelegateExpression( delegateType ) );

        return meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    public void Method( int x ) { }

    public void Method( string s ) { }

    public static void AcceptDelegate( MyIntAction action ) { }

    [DelegateAspect]
    public void Invoker() { }
}
