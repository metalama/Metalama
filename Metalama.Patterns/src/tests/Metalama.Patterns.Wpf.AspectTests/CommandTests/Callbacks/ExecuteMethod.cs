// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;

public class ExecuteMethod
{
    [Command]
    private void ExecuteInstanceNoParameters() { }

    [Command]
    private static void ExecuteStaticNoParameters() { }

    [Command]
    private void ExecuteInstanceWithParameter( int v ) { }

    [Command]
    private static void ExecuteStaticWithParameter( int v ) { }
}