// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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