// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// ReSharper disable ConvertToAutoPropertyWithInitializer
// ReSharper disable ConvertToAutoPropertyWhenPossible

namespace Metalama.Patterns.Observability.AspectTests.FieldKeyword_WithComputedProperty;

// <target>
[Observable]
public class FieldKeyword_WithComputedProperty
{
    // Semi-automatic property with validation.
    public string FirstName
    {
        get => field;
        set => field = value.Trim();
    } = "";

    // Semi-automatic property with validation.
    public string LastName
    {
        get => field;
        set => field = value.Trim();
    } = "";

    // Computed property that depends on semi-automatic properties.
    public string FullName => $"{this.FirstName} {this.LastName}";
}
