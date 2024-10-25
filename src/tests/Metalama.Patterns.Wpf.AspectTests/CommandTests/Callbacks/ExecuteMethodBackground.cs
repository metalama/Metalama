// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;

public class ExecuteMethodBackground
{
    [Command( Background = true )]
    private void ExecuteInstanceNoParameters() { }

    [Command( Background = true )]
    private static void ExecuteStaticNoParameters() { }

    [Command( Background = true )]
    private void ExecuteInstanceWithParameter( int v ) { }

    [Command( Background = true )]
    private static void ExecuteStaticWithParameter( int v ) { }
}