// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Contracts.AspectTests.Diagnostics;

public class Range_InvalidType_Byte2
{
    [StrictlyGreaterThan( 255 )]
    public byte Field { get; set; }
}