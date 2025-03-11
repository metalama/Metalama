// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Observability.UnitTests.Assets.Core;

namespace Metalama.Patterns.Observability.UnitTests.Assets.Initializers;

[Observable]
public partial class A
{
    /// <summary>
    /// Auto property with initializer 'new()'.
    /// </summary>
    public Simple A1 { get; set; } = new();

    /// <summary>
    /// Ref to A1.S1.
    /// </summary>
    public int RefA1S1 => this.A1.S1;
}