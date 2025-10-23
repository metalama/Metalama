// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.Introduced;
using System.Linq;

#pragma warning disable CS0169

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(IntroductionAspect), typeof(InvokerAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.Introduced;

/*
 * Tests invokers targeting a field introduced by previous aspect.
 */

public class InvokerAspect : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        builder.OverrideAccessors(
            nameof(this.GetTemplate),
            nameof(this.SetTemplate),
            new { target = builder.Target.DeclaringType.Fields.OfName( "Field" ).Single() } );
    }

    [Template]
    public dynamic? GetTemplate( [CompileTime] IFieldOrProperty target )
    {
        meta.InsertComment( "Invoke instance.Field" );
        _ = target.WithOptions( InvokerOptions.Base ).Value;
        meta.InsertComment( "Invoke instance.Field" );
        _ = target.WithOptions( InvokerOptions.Current ).Value;
        meta.InsertComment( "Invoke instance.Field" );
        _ = target.WithOptions( InvokerOptions.Final ).Value;

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate( [CompileTime] IFieldOrProperty target )
    {
        meta.InsertComment( "Invoke instance.Field" );
        target.WithOptions( InvokerOptions.Base ).Value = 42;
        meta.InsertComment( "Invoke instance.Field" );
        target.WithOptions( InvokerOptions.Current ).Value = 42;
        meta.InsertComment( "Invoke instance.Field" );
        target.WithOptions( InvokerOptions.Final ).Value = 42;

        meta.Proceed();
    }
}

public class IntroductionAspect : TypeAspect
{
    [Introduce]
    public int Field;
}

// <target>
[IntroductionAspect]
public class TargetClass
{
    [InvokerAspect]
    public int Invoker
    {
        get
        {
            return 0;
        }
        set { }
    }
}