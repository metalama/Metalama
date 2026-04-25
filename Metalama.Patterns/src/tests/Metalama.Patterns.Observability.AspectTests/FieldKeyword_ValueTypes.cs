// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

// ReSharper disable ConvertToAutoPropertyWithInitializer
// ReSharper disable ConvertToAutoPropertyWhenPossible

namespace Metalama.Patterns.Observability.AspectTests.FieldKeyword_ValueTypes;

// <target>
[Observable]
public class FieldKeyword_ValueTypes
{
    // Semi-automatic property with value type and validation.
    public int Age
    {
        get => field;
        set => field = value >= 0 ? value : throw new ArgumentOutOfRangeException( nameof(value) );
    }

    // Semi-automatic property with value type and clamping.
    public double Score
    {
        get => field;
        set => field = Math.Clamp( value, 0, 100 );
    }

    // Computed property depending on value-type semi-automatic properties.
    public string Summary => $"Age: {this.Age}, Score: {this.Score}";
}