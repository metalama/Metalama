// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;

public class CanExecuteMethodBackground
{
    [Command( Background = true )]
    private void ExecuteInstanceNoParameters() { }

    private bool CanExecuteInstanceNoParameters() => true;

    [Command( Background = true )]
    private static void ExecuteStaticNoParameters() { }

    private static bool CanExecuteStaticNoParameters() => true;

    [Command( Background = true )]
    private void ExecuteInstanceWithParameter( int v ) { }

    private bool CanExecuteInstanceWithParameter( int v ) => true;

    [Command( Background = true )]
    private static void ExecuteStaticWithParameter( int v ) { }

    private static bool CanExecuteStaticWithParameter( int v ) => true;
}