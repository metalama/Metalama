// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.Initializer_MethodInvoker;
using System;
using System.Linq;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(InitializerAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.Initializer_MethodInvoker;

/*
 * Tests that invokers in an initializer advice correctly resolve to the appropriate property version.
 * When using InvokerOptions.Base in an initializer, the invoker should resolve to the original property
 * (i.e. backing field), not the overridden version.
 */

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var property in builder.Target.Properties )
        {
            builder.With( property ).Override( nameof(this.PropertyTemplate) );
        }
    }

    [Template]
    public dynamic? PropertyTemplate
    {
        get
        {
            Console.WriteLine( "Overridden" );

            return meta.Proceed();
        }

        set
        {
            Console.WriteLine( "Overridden" );
            meta.Proceed();
        }
    }
}

public class InitializerAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(this.InitializerTemplate), InitializerKind.BeforeInstanceConstructor );
    }

    [Template]
    public void InitializerTemplate()
    {
        foreach ( var property in meta.Target.Type.Properties.OrderBy( p => p.Name ) )
        {
            // InvokerOptions.Base should access the property before overrides by this aspect layer.
            property.WithOptions( InvokerOptions.Base ).Value = 42;
        }

        foreach ( var property in meta.Target.Type.Properties.OrderBy( p => p.Name ) )
        {
            // InvokerOptions.Current should access the property after this aspect layer's changes.
            property.WithOptions( InvokerOptions.Current ).Value = 42;
        }

        foreach ( var property in meta.Target.Type.Properties.OrderBy( p => p.Name ) )
        {
            // InvokerOptions.Final should access the final overridden property.
            property.WithOptions( InvokerOptions.Final ).Value = 42;
        }
    }
}

// <target>
[Override]
[Initializer]
public class TestClass
{
    public int Property1 { get; set; }

    public int Property2 { get; set; }

    public TestClass() { }
}
