// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Include(Include/SimpleInpcByHand.cs)
#endif

using Metalama.Patterns.Observability.AspectTests.Include;

namespace Metalama.Patterns.Observability.AspectTests.InpcAutoPropertyWithInitializerWithRef;

[Observable]
public class InpcAutoPropertyWithInitializerWithRef
{
    public SimpleInpcByHand X { get; set; } = new( 42 );

    public int Y => this.X.A;
}