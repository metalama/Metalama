// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RemoveOutputCode
#endif

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable SA1401

// ReSharper disable UnusedMember.Global
// ReSharper disable UnassignedReadonlyField
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Metalama.Patterns.Immutability.AspectTests.ShallowAttributeAndWarnings;

[Immutable]
public class SomeClass
{
    // The following definitions should have a warning.
    public string MutableStringField; // Not read-only field.

    public string MutableProperty { get; set; } // Setter.

    // The following definitions should NOT have a warning.
    public readonly string ReadOnlyStringField;

    public string GetOnlyProperty { get; }

    public string InitOnlyProperty { get; init; }
}