// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Introspection.Navigation.Tests;

/// <summary>
/// A type whose properties are declared in an order that <c>PropertyComparer</c> changes.
/// </summary>
public sealed class Ordered
{
    public int Zeta { get; } = 1;

    public string Name { get; } = "n";

    public int Id { get; } = 2;

    public int[] Items { get; } = [1];
}
