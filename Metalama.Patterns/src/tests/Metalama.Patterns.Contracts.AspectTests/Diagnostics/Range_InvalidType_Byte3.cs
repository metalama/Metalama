// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Contracts.AspectTests.Diagnostics;

public class Range_InvalidType_Byte3
{
    private byte field;

    [return: Range( -50, -20 )]
    public byte GetField()
    {
        return this.field;
    }
}