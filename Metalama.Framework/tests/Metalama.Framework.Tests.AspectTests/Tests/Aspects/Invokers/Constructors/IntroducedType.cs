// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Constructors.IntroducedType;

public class InvokerAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var t = builder.With( builder.Target.DeclaringType! ).IntroduceClass( "IntroducedType" );

        var c = t.IntroduceConstructor( nameof(ConstructorTemplate) );

        builder.Override(
            nameof(Template),
            new { target = c.Declaration } );
    }

    [Template]
    public void ConstructorTemplate() { }

    [Template]
    public dynamic? Template( [CompileTime] IConstructor target )
    {
        meta.InsertComment( "Invoke new <introduced>();" );
        target.Invoke();

        return meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    [InvokerAspect]
    public void Invoker() { }
}