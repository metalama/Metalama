// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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