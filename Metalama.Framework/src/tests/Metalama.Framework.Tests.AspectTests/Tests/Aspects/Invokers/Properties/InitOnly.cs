// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.InitOnly;
using System;
using System.Linq;
using Metalama.Framework.Code.Invokers;

[assembly: AspectOrder(
    AspectOrderDirection.CompileTime,
    typeof(IntroduceFieldAttribute),
    typeof(InvokeBeforeAttribute),
    typeof(OverridePropertyAttribute),
    typeof(InvokeAfterAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.InitOnly;

/*
 * Tests invokers targeting an init-only property.
 */
#pragma warning disable CS0414

public class IntroduceFieldAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceProperty( nameof(PropertyTemplate), buildProperty: f => f.Name = "IntroducedProperty" );
        var p = builder.IntroduceProperty( nameof(PropertyTemplate), buildProperty: f => f.Name = "OverriddenIntroducedProperty" );

        p.AddAspect<OverridePropertyAttribute>();
    }

    [Template]
    public int PropertyTemplate { get; init; }
}

public class OverridePropertyAttribute : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
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

public class InvokeBeforeAttribute : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.Override( nameof(Template) );
    }

    [Template]
    public void Template()
    {
        meta.InsertComment( "--- Before ---" );

        meta.InsertComment( "Base" );

        foreach (var fieldOrProperty in meta.Target.Constructor.DeclaringType.Properties.OrderBy( f => f.Name ))
        {
            fieldOrProperty.With( InvokerOptions.Base ).Value = 42;
        }

        meta.InsertComment( "Current" );

        foreach (var fieldOrProperty in meta.Target.Constructor.DeclaringType.Properties.OrderBy( f => f.Name ))
        {
            fieldOrProperty.With( InvokerOptions.Current ).Value = 42;
        }

        meta.InsertComment( "Final" );

        foreach (var fieldOrProperty in meta.Target.Constructor.DeclaringType.Properties.OrderBy( f => f.Name ))
        {
            fieldOrProperty.With( InvokerOptions.Final ).Value = 42;
        }

        meta.Proceed();
    }
}

public class InvokeAfterAttribute : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.Override( nameof(Template) );
    }

    [Template]
    public void Template()
    {
        meta.Proceed();

        meta.InsertComment( "--- After ---" );

        meta.InsertComment( "Base" );

        foreach (var fieldOrProperty in meta.Target.Constructor.DeclaringType.Properties.OrderBy( f => f.Name ))
        {
            fieldOrProperty.With( InvokerOptions.Base ).Value = 42;
        }

        meta.InsertComment( "Current" );

        foreach (var fieldOrProperty in meta.Target.Constructor.DeclaringType.Properties.OrderBy( f => f.Name ))
        {
            fieldOrProperty.With( InvokerOptions.Current ).Value = 42;
        }

        meta.InsertComment( "Final" );

        foreach (var fieldOrProperty in meta.Target.Constructor.DeclaringType.Properties.OrderBy( f => f.Name ))
        {
            fieldOrProperty.With( InvokerOptions.Final ).Value = 42;
        }
    }
}

// <target>
[IntroduceField]
public class TestClass
{
    public int Property { get; init; }

    [OverrideProperty]
    public int OverriddenProperty { get; init; }

    [InvokeBefore]
    [InvokeAfter]
    public TestClass() { }
}

// <target>
public class UsingClass
{
    public void Foo()
    {
        new TestClass()
        {
            Property = 42, OverriddenProperty = 42,
#if TESTRUNNER
            IntroducedProperty = 42,
            OverriddenIntroducedProperty = 42,
#endif
        };
    }
}