// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_InitOnly;

/// <summary>
/// Tests overriding init-only properties with a field keyword template.
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
        init
        {
            Console.WriteLine( $"Init {meta.Target.Property.Name} to {value}" );
            field = value;
        }
    }
}

// <target>
[LoggingAspect]
internal class C
{
    public string? Name { get; init; }

    public string? Title { get; init; } = "Default";
}

#endif
