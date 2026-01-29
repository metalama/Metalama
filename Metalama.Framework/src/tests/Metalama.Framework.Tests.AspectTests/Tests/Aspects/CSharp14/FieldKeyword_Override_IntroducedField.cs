// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
