// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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