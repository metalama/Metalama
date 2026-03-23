// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_Generic;

/*
 * Tests CreateDelegateExpression targeting a non-overloaded generic method.
 * Expected: method group expression with type arguments (this.Method<int>).
 */

public class DelegateAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var genericMethod = builder.Target.DeclaringType!.Methods.OfName( "GenericMethod" ).Single();
        var constructedMethod = genericMethod.MakeGenericInstance( builder.Target.Compilation.Factory.GetTypeByReflectionType( typeof(int) ) );

        builder.Override(
            nameof(this.Template),
            new { target = constructedMethod } );
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
    public void GenericMethod<T>( T value ) { }

    [DelegateAspect]
    public void Invoker() { }
}
