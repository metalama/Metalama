// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_ExplicitDelegateType;

/*
 * Tests CreateDelegateExpression with an explicit delegateType parameter.
 * Expected: wraps the method group in `new MyDelegate(this.Method)` using the specified delegate type.
 */

public delegate void MyDelegate( int x );

public class DelegateAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var targetMethod = builder.Target.DeclaringType!.Methods.OfName( "Method" )
            .Single( m => m.Parameters.Count == 1 && m.Parameters[0].Type.SpecialType == SpecialType.Int32 );

        var delegateType = (INamedType) builder.Target.Compilation.Factory.GetTypeByReflectionType( typeof(MyDelegate) );

        builder.Override(
            nameof(this.Template),
            new { target = targetMethod, delegateType } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IMethod target, [CompileTime] INamedType delegateType )
    {
        var action = target.CreateDelegateExpression( delegateType ).Value;

        return meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    public void Method( int x ) { }

    public void Method( string s ) { }

    [DelegateAspect]
    public void Invoker() { }
}
