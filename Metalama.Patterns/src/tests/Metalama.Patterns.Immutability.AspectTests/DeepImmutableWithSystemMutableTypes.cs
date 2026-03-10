// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
#if TEST_OPTIONS
// @RemoveOutputCode
#endif

#pragma warning disable CS8618
#pragma warning disable SA1401

// ReSharper disable UnusedMember.Global
// ReSharper disable UnassignedReadonlyField

namespace Metalama.Patterns.Immutability.AspectTests.AttributeAndWarnings.DeepImmutableWithSystemMutableTypes;

[Immutable( ImmutabilityKind.Deep )]
public class DeeplyImmutableWithMutableSystemTypes
{
    // The following definitions should have a warning because these types are not deeply immutable.
    public readonly (int, string) ValueTupleField;

    public readonly Memory<byte> MemoryField;

    // The following definitions should NOT have a warning.
    public readonly DateTime DateTimeField;

    public readonly int IntField;
}
