// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Eligibility;

internal class MustHaveZeroOrOneParameter
{
    [Command]
    private void ZeroParameters() { }

    [Command]
    private void OneParameter( int x ) { }

    [Command]
    private void TwoParameters( int x, int y ) { }
}