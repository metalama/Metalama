// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Eligibility;

#pragma warning disable VSTHRD200

internal class MustHaveValidParametersAsync
{
    [Command]
    private Task ZeroParametersTask() => Task.CompletedTask;

    [Command]
    private Task ArgumentOnlyTask( int x ) => Task.CompletedTask;

    [Command]
    private Task CancellationTokenOnlyTask( CancellationToken cancellationToken ) => Task.CompletedTask;

    [Command]
    private Task ArgumentAndCancellationTokenTask( int x, CancellationToken cancellationToken ) => Task.CompletedTask;

    [Command]
    private Task TwoParametersAsync( int x, int y ) => Task.CompletedTask;

    [Command]
    private Task ThreeParametersAsync( int x, int y, CancellationToken cancellationToken ) => Task.CompletedTask;
}