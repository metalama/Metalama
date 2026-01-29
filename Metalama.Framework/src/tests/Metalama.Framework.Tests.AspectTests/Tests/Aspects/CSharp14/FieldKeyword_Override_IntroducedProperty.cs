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

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_IntroducedProperty.LoggingAspect),
    typeof(Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_IntroducedProperty.IntroducePropertyAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_IntroducedProperty;

/// <summary>
/// Introduces a property.
/// </summary>
internal class IntroducePropertyAspect : TypeAspect
{
    [Introduce]
    public string? Name { get; set; }
}

/// <summary>
/// Overrides properties using a semi-auto template.
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
[IntroducePropertyAspect]
[LoggingAspect]
internal class C { }

#endif
