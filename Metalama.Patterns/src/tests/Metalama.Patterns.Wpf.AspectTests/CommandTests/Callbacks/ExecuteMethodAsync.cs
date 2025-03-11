// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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