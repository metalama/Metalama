// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;

public class CanExecutePropertysync
{
    [Command]
    private Task ExecuteInstanceAsync() => Task.CompletedTask;

    private bool CanExecuteInstance => true;

    [Command]
    private static Task ExecuteStaticAsync() => Task.CompletedTask;

    private static bool CanExecuteStatic => true;
}