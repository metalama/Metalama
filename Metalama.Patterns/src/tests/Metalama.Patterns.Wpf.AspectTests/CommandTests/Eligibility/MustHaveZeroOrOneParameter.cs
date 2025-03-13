// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Eligibility;

internal class MustHaveZeroOrOneParameter
{
    [Command]
    private void ZeroParameters() { }

    [Command]
    private void OneParameter( int x ) { }

    [Command]
    private void TwoParameters( int x, int y ) { }
}