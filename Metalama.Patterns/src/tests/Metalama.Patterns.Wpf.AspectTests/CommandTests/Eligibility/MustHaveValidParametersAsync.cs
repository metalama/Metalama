// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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