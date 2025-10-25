// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System.Linq;

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.DifferentClass;

/*
 * Tests default and final invokers targeting a field declared in a different class.
 */

public class InvokerAspect : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        builder.OverrideAccessors(
            nameof(this.GetTemplate),
            nameof(this.SetTemplate),
            new { target = ((INamedType) builder.Target.DeclaringType.Fields.Single().Type).FieldsAndProperties.OfName( "Field" ).Single() } );
    }

    [Template]
    public dynamic? GetTemplate( [CompileTime] IFieldOrProperty target )
    {
        meta.InsertComment( "Invoke instance.Field" );
        _ = target.WithObject( meta.Target.FieldOrProperty.DeclaringType.Fields.Single()).Value;
        meta.InsertComment( "Invoke instance?.Field" );

        _ = target.WithObject( meta.Target.FieldOrProperty.DeclaringType.Fields.Single())
            .WithOptions( InvokerOptions.NullConditional )
            .Value;

        meta.InsertComment( "Invoke instance.Field" );
        _ = target.WithObject( meta.Target.FieldOrProperty.DeclaringType.Fields.Single()).WithOptions( InvokerOptions.Final ).Value;
        meta.InsertComment( "Invoke instance?.Field" );

        _ = target.WithObject( meta.Target.FieldOrProperty.DeclaringType.Fields.Single()).WithOptions( InvokerOptions.Final | InvokerOptions.NullConditional )
            .Value;

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate( [CompileTime] IFieldOrProperty target )
    {
        meta.InsertComment( "Invoke instance.Field" );
        target.WithObject( meta.Target.FieldOrProperty.DeclaringType.Fields.Single()).Value = 42;
        meta.InsertComment( "Invoke instance.Field" );
        target.WithObject( meta.Target.FieldOrProperty.DeclaringType.Fields.Single()).WithOptions( InvokerOptions.Final ).Value = 42;

        meta.Proceed();
    }
}

public class DifferentClass
{
    public int Field;
}

// <target>
public class TargetClass
{
    private DifferentClass? instance;

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