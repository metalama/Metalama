// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden;
using System.Linq;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(InvokerAfterAspect), typeof(IntroductionAspect), typeof(InvokerBeforeAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic_AspectHidden;

/*
 * Tests invokers targeting a property declared in the base class which is hidden by an aspect-introduced field:
 */

public class InvokerBeforeAspect : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        builder.OverrideAccessors(
            nameof(this.GetTemplate),
            nameof(this.SetTemplate),
            new { target = builder.Target.DeclaringType!.BaseType!.FieldsAndProperties.OfName( "Field" ).Single() } );
    }

    [Template]
    public dynamic? GetTemplate( [CompileTime] IFieldOrProperty target )
    {
        meta.InsertComment( "Invoke TargetClass.Field" );
        _ = target.Value;
        meta.InsertComment( "Invoke BaseClass.Field" );
        _ = target.WithOptions( InvokerOptions.Base ).Value;
        meta.InsertComment( "Invoke BaseClass.Field" );
        _ = target.WithOptions( InvokerOptions.Current ).Value;
        meta.InsertComment( "Invoke TargetClass.Field" );
        _ = target.WithOptions( InvokerOptions.Final ).Value;

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate( [CompileTime] IFieldOrProperty target )
    {
        meta.InsertComment( "Invoke TargetClass.Field" );
        target.Value = 42;
        meta.InsertComment( "Invoke BaseClass.Field" );
        target.WithOptions( InvokerOptions.Base ).Value = 42;
        meta.InsertComment( "Invoke BaseClass.Field" );
        target.WithOptions( InvokerOptions.Current ).Value = 42;
        meta.InsertComment( "Invoke TargetClass.Field" );
        target.WithOptions( InvokerOptions.Final ).Value = 42;

        meta.Proceed();
    }
}

public class IntroductionAspect : TypeAspect
{
    [Introduce( WhenExists = OverrideStrategy.New )]
    public static int Field;
}

public class InvokerAfterAspect : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        builder.OverrideAccessors(
            nameof(this.GetTemplate),
            nameof(this.SetTemplate),
            new { target = builder.Target.DeclaringType!.AllFieldsAndProperties.OfName( "Field" ).Single() } );
    }

    [Template]
    public dynamic? GetTemplate( [CompileTime] IFieldOrProperty target )
    {
        meta.InsertComment( "Invoke TargetClass.Field" );
        _ = target.Value;
        meta.InsertComment( "Invoke TargetClass.Field" );
        _ = target.WithOptions( InvokerOptions.Base ).Value;
        meta.InsertComment( "Invoke TargetClass.Field" );
        _ = target.WithOptions( InvokerOptions.Current ).Value;
        meta.InsertComment( "Invoke TargetClass.Field" );
        _ = target.WithOptions( InvokerOptions.Final ).Value;

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate( [CompileTime] IFieldOrProperty target )
    {
        meta.InsertComment( "Invoke TargetClass.Field" );
        target.Value = 42;
        meta.InsertComment( "Invoke TargetClass.Field" );
        target.WithOptions( InvokerOptions.Base ).Value = 42;
        meta.InsertComment( "Invoke TargetClass.Field" );
        target.WithOptions( InvokerOptions.Current ).Value = 42;
        meta.InsertComment( "Invoke TargetClass.Field" );
        target.WithOptions( InvokerOptions.Final ).Value = 42;

        meta.Proceed();
    }
}

public class BaseClass
{
    public static int Field;
}

// <target>
[IntroductionAspect]
public class TargetClass : BaseClass
{
    [InvokerBeforeAspect]
    public int InvokerBefore
    {
        get
        {
            return 0;
        }
        set { }
    }

    [InvokerAfterAspect]
    public int InvokerAfter
    {
        get
        {
            return 0;
        }
        set { }
    }
}