// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;

public class CanExecuteProperty
{
    [Command]
    private void ExecuteInstance() { }

    private bool CanExecuteInstance => true;

    [Command]
    private static void ExecuteStatic() { }

    private static bool CanExecuteStatic => true;
}