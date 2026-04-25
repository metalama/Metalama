// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

// ReSharper disable ConvertToAutoPropertyWithInitializer
// ReSharper disable ConvertToAutoPropertyWhenPossible

namespace Metalama.Patterns.Observability.AspectTests.FieldKeyword_WithInitializer;

// <target>
[Observable]
public class FieldKeyword_WithInitializer
{
    // Semi-automatic property with field keyword and initializer.
    public string Name
    {
        get => field;
        set => field = value.Trim();
    } = "Default";

    // Semi-automatic property with value type, validation, and initializer.
    public int Count
    {
        get => field;
        set => field = Math.Max( value, 0 );
    } = 10;

    // Computed property depending on initialized semi-automatic properties.
    public string Display => $"{this.Name}: {this.Count}";
}
