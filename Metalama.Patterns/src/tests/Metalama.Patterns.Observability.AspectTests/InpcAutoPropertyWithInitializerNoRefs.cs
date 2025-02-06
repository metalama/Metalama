// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @Include(Include/SimpleInpcByHand.cs)
#endif

using Metalama.Patterns.Observability.AspectTests.Include;

namespace Metalama.Patterns.Observability.AspectTests.InpcAutoPropertyWithInitializerNoRefs;

[Observable]
public class InpcAutoPropertyWithInitializerNoRefs
{
    public SimpleInpcByHand X { get; set; } = new( 42 );
}