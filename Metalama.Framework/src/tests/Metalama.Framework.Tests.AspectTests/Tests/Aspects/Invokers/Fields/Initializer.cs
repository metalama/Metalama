// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.Initializer;
using System;
using System.Linq;
using Metalama.Framework.Code.Invokers;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAndInitializeAttribute), typeof(ResetInitializerAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.Initializer;

/*
 * Tests invokers targeting a field with an initializer.
 */

public class ResetInitializerAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var f = builder.Target.Fields.OfName( "TestField" ).Single();
        var p = builder.Target.Properties.OfName( "TestProperty" ).Single();

        builder.AddInitializer( nameof(Template), InitializerKind.BeforeInstanceConstructor, args: new { fieldOrProperty = f } );
        builder.AddInitializer( nameof(Template), InitializerKind.BeforeInstanceConstructor, args: new { fieldOrProperty = p } );
    }

    [Template]
    public void Template( [CompileTime] IFieldOrProperty fieldOrProperty )
    {
        fieldOrProperty.With( InvokerOptions.Base ).Value = default;
    }
}

public class OverrideAndInitializeAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var f = builder.Target.Fields.OfName( "TestField" ).Single();
        var p = builder.Target.Properties.OfName( "TestProperty" ).Single();

        builder.With( f ).Override( nameof(Template) );
        builder.With( p ).Override( nameof(Template) );
    }

    [Template]
    public dynamic? Template
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

// <target>
[ResetInitializer]
[OverrideAndInitialize]
public class TestClass
{
    public int TestField;

    public int TestProperty { get; set; }
}