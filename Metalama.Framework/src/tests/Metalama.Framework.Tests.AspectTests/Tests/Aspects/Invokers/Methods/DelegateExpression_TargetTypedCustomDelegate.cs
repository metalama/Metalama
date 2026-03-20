// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_TargetTypedCustomDelegate;

/*
 * Tests CreateDelegateExpression with a custom delegate type passed as the delegateType parameter.
 * When passing an overloaded method's delegate expression to a method expecting a custom delegate type,
 * the delegateType parameter must be used to generate the correct disambiguation expression.
 * Expected: new MyIntAction(this.Method)
 */

public delegate void MyIntAction( int x );

public class DelegateAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var targetInt = builder.Target.DeclaringType!.Methods.OfName( "Method" )
            .Single( m => m.Parameters.Count == 1 && m.Parameters[0].Type.SpecialType == SpecialType.Int32 );

        var delegateType = (INamedType) builder.Target.Compilation.Factory.GetTypeByReflectionType( typeof(MyIntAction) );

        builder.Override(
            nameof(this.Template),
            new { target = targetInt, delegateType } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IMethod target, [CompileTime] INamedType delegateType )
    {
        // Use the delegateType parameter to generate correct disambiguation for a custom delegate.
        TargetClass.AcceptDelegate( target.CreateDelegateExpression( delegateType ).Value );

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
