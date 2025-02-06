// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Observability.AspectTests.NonInpcAutoPropertyWithInitializer;

[Observable]
public class NonInpcAutoPropertyWithInitializer
{
    public int X { get; set; } = 42;
}