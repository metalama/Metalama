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

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_Record;

/// <summary>
/// Tests overriding record properties with a field keyword template.
/// </summary>
internal class LoggingAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var property in builder.Target.Properties )
        {
            if ( property.Name != "EqualityContract" )
            {
                builder.With( property ).Override( nameof(PropertyTemplate) );
            }
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

#pragma warning disable CS8907 // Parameter is unread. Did you forget to use it to initialize the property with that name?

// <target>
[LoggingAspect]
internal record C( string? Name, string? Title )
{
    public string? Description { get; init; }
}

#pragma warning restore CS8907

#endif
