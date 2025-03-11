// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassVirtual_DifferentInstance_Base;

/*
 * Tests base invoker targeting a method from a different instance.
 */

public class InvokerAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var anotherMethodBase = builder.Target.DeclaringType!.BaseType!.Methods.OfName( "Method" ).Single();

        builder.Override(
            nameof(Template),
            new { target = anotherMethodBase } );
    }

    [Template]
    public dynamic? Template( IMethod target )
    {
        target.With( (IExpression)meta.Target.Method.Parameters[0].Value!, InvokerOptions.Base ).Invoke();

        return meta.Proceed();
    }

    [Template]
    public void AnotherMethodTemplate() { }
}

public class BaseClass
{
    public virtual void Method() { }
}

// <target>
public class TargetClass : BaseClass
{
    [InvokerAspect]
    public void Invoker( TargetClass instance ) { }
}