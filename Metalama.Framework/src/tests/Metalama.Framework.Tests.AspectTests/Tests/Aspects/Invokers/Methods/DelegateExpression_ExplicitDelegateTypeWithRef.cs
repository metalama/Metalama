// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_ExplicitDelegateTypeWithRef;

/*
 * Tests CreateDelegateExpression with an explicit delegateType parameter for an overloaded method
 * with a ref parameter. Without the explicit delegate type, this would throw because Action/Func
 * cannot represent ref parameters. With the explicit delegate type, it should succeed.
 */

public delegate void RefDelegate( ref int x );

public class DelegateAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var targetMethod = builder.Target.DeclaringType!.Methods.OfName( "Method" )
            .Single( m => m.Parameters.Count == 1 && m.Parameters[0].RefKind == RefKind.Ref );

        var delegateType = (INamedType) builder.Target.Compilation.Factory.GetTypeByReflectionType( typeof(RefDelegate) );

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
    public void Method( ref int x ) { x++; }

    public void Method( string s ) { }

    [DelegateAspect]
    public void Invoker() { }
}
