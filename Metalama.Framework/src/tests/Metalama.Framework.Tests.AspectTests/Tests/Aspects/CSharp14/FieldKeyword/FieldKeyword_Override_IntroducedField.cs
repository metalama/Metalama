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

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_IntroducedField.LoggingAspect),
    typeof(Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_IntroducedField.IntroduceFieldAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_IntroducedField;

/// <summary>
/// Introduces a field that will be promoted to a property.
/// </summary>
internal class IntroduceFieldAspect : TypeAspect
{
    [Introduce]
    public string? _name;
}

/// <summary>
/// Overrides the introduced field (promoted to property) with a field keyword template.
/// </summary>
internal class LoggingAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var field in builder.Target.Fields )
        {
            builder.With( field ).Override( nameof(FieldTemplate) );
        }
    }

    [Template]
    public string? FieldTemplate
    {
        get => field;
        set
        {
            Console.WriteLine( $"Setting {meta.Target.FieldOrProperty.Name} to {value}" );
            field = value;
        }
    }
}

// <target>
[IntroduceFieldAspect]
[LoggingAspect]
internal class C { }

#endif
