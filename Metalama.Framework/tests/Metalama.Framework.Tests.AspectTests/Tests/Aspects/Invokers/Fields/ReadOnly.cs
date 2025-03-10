// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.ReadOnly;
using System;
using System.Linq;
using Metalama.Framework.Code.Invokers;

[assembly: AspectOrder(
    AspectOrderDirection.CompileTime,
    typeof(IntroduceFieldAttribute),
    typeof(InvokeBeforeAttribute),
    typeof(OverrideFieldAttribute),
    typeof(InvokeAfterAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.ReadOnly;

/*
 * Tests invokers targeting an read-only field.
 */
#pragma warning disable CS0169, CS0649

public class IntroduceFieldAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceField( nameof(FieldTemplate), buildField: f => f.Name = "IntroducedField" );
        var f = builder.IntroduceField( nameof(FieldTemplate), buildField: f => f.Name = "OverriddenIntroducedField" );

        f.AddAspect<OverrideFieldAttribute>();
    }

    [Template]
    private readonly int FieldTemplate;
}

public class OverrideFieldAttribute : OverrideFieldOrPropertyAspect
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

        foreach (var fieldOrProperty in meta.Target.Constructor.DeclaringType.FieldsAndProperties.OrderBy( f => f.Name ))
        {
            fieldOrProperty.With( InvokerOptions.Base ).Value = 42;
        }

        meta.InsertComment( "Current" );

        foreach (var fieldOrProperty in meta.Target.Constructor.DeclaringType.FieldsAndProperties.OrderBy( f => f.Name ))
        {
            fieldOrProperty.With( InvokerOptions.Current ).Value = 42;
        }

        meta.InsertComment( "Final" );

        foreach (var fieldOrProperty in meta.Target.Constructor.DeclaringType.FieldsAndProperties.OrderBy( f => f.Name ))
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

        foreach (var fieldOrProperty in meta.Target.Constructor.DeclaringType.FieldsAndProperties.OrderBy( f => f.Name ))
        {
            fieldOrProperty.With( InvokerOptions.Base ).Value = 42;
        }

        meta.InsertComment( "Current" );

        foreach (var fieldOrProperty in meta.Target.Constructor.DeclaringType.FieldsAndProperties.OrderBy( f => f.Name ))
        {
            fieldOrProperty.With( InvokerOptions.Current ).Value = 42;
        }

        meta.InsertComment( "Final" );

        foreach (var fieldOrProperty in meta.Target.Constructor.DeclaringType.FieldsAndProperties.OrderBy( f => f.Name ))
        {
            fieldOrProperty.With( InvokerOptions.Final ).Value = 42;
        }
    }
}

// <target>
[IntroduceField]
public class TestClass
{
    private readonly int Field;

    [OverrideField]
    private readonly int OverriddenField;

    [InvokeBefore]
    [InvokeAfter]
    public TestClass() { }
}