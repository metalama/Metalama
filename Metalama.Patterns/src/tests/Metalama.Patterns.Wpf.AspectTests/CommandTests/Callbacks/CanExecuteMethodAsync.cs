// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;

public class CanExecuteMethodAsync
{
    [Command]
    private Task ExecuteInstanceNoParametersAsync() => Task.CompletedTask;

    private bool CanExecuteInstanceNoParameters() => true;

    [Command]
    private static Task ExecuteStaticNoParametersAsync() => Task.CompletedTask;

    private static bool CanExecuteStaticNoParameters() => true;

    [Command]
    private Task ExecuteInstanceWithParameterAsync( int v ) => Task.CompletedTask;

    private bool CanExecuteInstanceWithParameter( int v ) => true;

    [Command]
    private static Task ExecuteStaticWithParameterAsync( int v ) => Task.CompletedTask;

    private static bool CanExecuteStaticWithParameter( int v ) => true;
}