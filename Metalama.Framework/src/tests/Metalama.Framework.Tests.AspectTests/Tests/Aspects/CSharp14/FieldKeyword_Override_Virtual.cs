// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_Virtual;

/// <summary>
/// Tests overriding virtual/override properties with a field keyword template.
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

internal class BaseClass
{
    public virtual string? BaseName { get; set; }
}

// <target>
[LoggingAspect]
internal class C : BaseClass
{
    public virtual string? Name { get; set; }

    public override string? BaseName { get; set; }
}

#endif
