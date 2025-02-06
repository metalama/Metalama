// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Observability.AspectTests.Include;

#if TEST_OPTIONS
// @Include(Include/SimpleInpcByHand.cs)
#endif

namespace Metalama.Patterns.Observability.AspectTests.InpcAutoPropertyNoRefs;

// <target>
[Observable]
public class InpcAutoPropertyNoRefs
{
    public SimpleInpcByHand X { get; set; }
}