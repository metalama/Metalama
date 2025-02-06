// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;

public class CanExecutePropertyBackground
{
    [Command( Background = true )]
    private void ExecuteInstance() { }

    private bool CanExecuteInstance => true;

    [Command( Background = true )]
    private static void ExecuteStatic() { }

    private static bool CanExecuteStatic => true;
}