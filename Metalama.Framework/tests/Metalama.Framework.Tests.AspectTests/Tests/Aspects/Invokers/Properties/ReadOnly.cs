using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.ReadOnly;
using System;
using System.Linq;
using Metalama.Framework.Code.Invokers;

[assembly: AspectOrder(
    AspectOrderDirection.CompileTime,
    typeof(IntroduceFieldAttribute),
    typeof(InvokeBeforeAttribute),
    typeof(OverridePropertyAttribute),
    typeof(InvokeAfterAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.ReadOnly;

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
    public int PropertyTemplate { get; }
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
    public int Property { get; }

    [OverrideProperty]
    public int OverriddenProperty { get; }

    [InvokeBefore]
    [InvokeAfter]
    public TestClass() { }
}