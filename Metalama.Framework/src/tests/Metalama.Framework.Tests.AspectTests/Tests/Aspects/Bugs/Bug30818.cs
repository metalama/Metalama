// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug30818;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(ValidationAspect), typeof(OnPropertyChangedAspect) )]

#pragma warning disable CS8618, CS8602

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug30818;

internal class ValidationAspect : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        builder.AddContract(
            nameof(ValidatePropertyGetter),
            ContractDirection.Output,
            args: new { propertyName = builder.Target.Name } );

        builder.AddContract(
            nameof(ValidatePropertySetter),
            ContractDirection.Input,
            args: new { propertyName = builder.Target.Name } );
    }

    [Template]
    private void ValidatePropertySetter( dynamic? value, [CompileTime] string propertyName )
    {
        if (value is not null)
        {
            throw new Exception( $"The property '{propertyName}' must not be set to null!" );
        }
    }

    [Template]
    private void ValidatePropertyGetter( dynamic? value, [CompileTime] string propertyName )
    {
        if (value is not null)
        {
            throw new Exception( $"The property '{propertyName}' must not be set to null!" );
        }
    }
}

public sealed class OnPropertyChangedAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var property in builder.AdvisedTarget.FieldsAndProperties.Where( f => !f.IsImplicitlyDeclared ))
        {
            builder.With( property ).OverrideAccessors( null, nameof(OverridePropertySetter) );
        }
    }

    [Template]
    public void OverridePropertySetter( dynamic? value )
    {
        if (meta.Target.FieldOrProperty.Value == value)
        {
            return;
        }

        OnChanged( meta.Target.FieldOrProperty.Name, meta.Target.FieldOrProperty.Value, value );
        meta.Proceed();
    }

    [Introduce( WhenExists = OverrideStrategy.Ignore )]
    private void OnChanged( string propertyName, object oldValue, object newValue ) { }
}

// <target>
[OnPropertyChangedAspect]
internal class Foo
{
    [ValidationAspect]
    public string Name { get; set; } = null!;
}