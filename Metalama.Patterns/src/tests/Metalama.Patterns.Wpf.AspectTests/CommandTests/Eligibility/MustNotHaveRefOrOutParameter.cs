// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Eligibility;

internal class MustNotHaveRefOrOutParameter
{
    [Command]
    private void RefParameter( ref int x ) { }

    [Command]
    private void OutParameter( out int x ) { x = default; }
}