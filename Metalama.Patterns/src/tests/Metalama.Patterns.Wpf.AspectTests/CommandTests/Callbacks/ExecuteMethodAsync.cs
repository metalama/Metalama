// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;

public class ExecuteMethodAsync
{
    [Command]
    private Task ExecuteInstanceNoParametersAsync() => Task.CompletedTask;

    [Command]
    private static Task ExecuteStaticNoParametersAsync() => Task.CompletedTask;

    [Command]
    private Task ExecuteInstanceWithParameterAsync( int v ) => Task.CompletedTask;

    [Command]
    private static Task ExecuteStaticWithParameterAsync( int v ) => Task.CompletedTask;

    [Command]
    private Task ExecuteInstanceWithCancellationTokenAsync( CancellationToken cancellationToken ) => Task.CompletedTask;

    [Command]
    private static Task ExecuteStaticWithCancellationTokenAsync( CancellationToken cancellationToken ) => Task.CompletedTask;

    [Command]
    private Task ExecuteInstanceWithCancellationTokenAndParameterAsync( int v, CancellationToken cancellationToken ) => Task.CompletedTask;

    [Command]
    private static Task ExecuteStaticWithCancellationTokenAndParameterAsync( int v, CancellationToken cancellationToken ) => Task.CompletedTask;
}