// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Observability.AspectTests.Include;

#if TEST_OPTIONS
// @Include(Include/SimpleInpcByHand.cs)
#endif

namespace Metalama.Patterns.Observability.AspectTests.InpcAutoPropertyWithRef;

// <target>
[Observable]
public class InpcAutoPropertyWithRef
{
    public SimpleInpcByHand X { get; set; }

    public int Y => this.X.A;
}