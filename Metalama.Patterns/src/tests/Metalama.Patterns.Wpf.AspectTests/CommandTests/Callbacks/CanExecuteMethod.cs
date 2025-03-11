// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;

public class CanExecuteMethod
{
    [Command]
    private void ExecuteInstanceNoParameters() { }

    private bool CanExecuteInstanceNoParameters() => true;

    [Command]
    private static void ExecuteStaticNoParameters() { }

    private static bool CanExecuteStaticNoParameters() => true;

    [Command]
    private void ExecuteInstanceWithParameter( int v ) { }

    private bool CanExecuteInstanceWithParameter( int v ) => true;

    [Command]
    private static void ExecuteStaticWithParameter( int v ) { }

    private static bool CanExecuteStaticWithParameter( int v ) => true;
}