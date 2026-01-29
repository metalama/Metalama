// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_ChainedSemiAuto.LoggingAspect),
    typeof(Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_ChainedSemiAuto.ValidationAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_ChainedSemiAuto;

/// <summary>
/// First aspect that overrides properties with a semi-auto template introducing a backing field.
/// </summary>
internal class ValidationAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var property in builder.Target.Properties )
        {
            builder.With( property ).Override( nameof(PropertyTemplate) );
        }
    }

    [Template]
    public string? PropertyTemplate
    {
        get => field;
        set
        {
            if ( string.IsNullOrEmpty( value ) )
            {
                throw new ArgumentException( $"{meta.Target.Property.Name} cannot be null or empty" );
            }
            field = value;
        }
    }
}

/// <summary>
/// Second aspect that also overrides properties with a semi-auto template.
/// This runs after ValidationAspect, so it must use the backing field introduced by ValidationAspect.
/// </summary>
internal class LoggingAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var property in builder.Target.Properties )
        {
            builder.With( property ).Override( nameof(PropertyTemplate) );
        }
    }

    [Template]
    public string? PropertyTemplate
    {
        get => field;
        set
        {
            Console.WriteLine( $"Setting {meta.Target.Property.Name} to {value}" );
            field = value;
        }
    }
}

// <target>
[ValidationAspect]
[LoggingAspect]
internal class C
{
    public string? Name { get; set; }
}

#endif
