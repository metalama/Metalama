// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Eligibility;

internal class MustReturnVoid
{
    [Command]
    private void ReturnsVoid() { }

    [Command]
    private int ReturnsInt() => 42;

    [Command]
    private Task<int> ReturnsIntAsync() => Task.FromResult( 42 );
}