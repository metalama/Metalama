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

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_AfterOverride.TracingAspect),
    typeof(Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_AfterOverride.ValidationAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_AfterOverride;

/// <summary>
/// First aspect (innermost) that overrides properties using a semi-auto template with field keyword.
/// This introduces a backing field and provides validation.
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
/// Second aspect (outermost) that overrides properties with a regular template calling meta.Proceed().
/// This adds tracing on top of the validation.
/// </summary>
internal class TracingAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var property in builder.Target.Properties )
        {
            builder.With( property ).Override( nameof(PropertyTemplate) );
        }
    }

    [Template]
    public dynamic? PropertyTemplate
    {
        get
        {
            Console.WriteLine( $"Trace get {meta.Target.Property.Name}" );
            return meta.Proceed();
        }
        set
        {
            Console.WriteLine( $"Trace set {meta.Target.Property.Name}" );
            meta.Proceed();
        }
    }
}

// <target>
[ValidationAspect]
[TracingAspect]
internal class C
{
    public string? Name { get; set; }
}

#endif
