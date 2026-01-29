// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_Field;

// Test overriding a field with a template that uses the field keyword.
// The field gets promoted to a property, and a new backing field is created.
public class LoggingAspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get => field;
        set
        {
            Console.WriteLine( $"Setting to {value}" );
            field = value;
        }
    }
}

// <target>
internal class TargetClass
{
    [LoggingAspect]
    public string? Name;

    [LoggingAspect]
    public int Count;
}

#endif
