// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// ReSharper disable UnusedTypeParameter

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Eligibility;

internal class MustNotBeGeneric
{
    [Command]
    private void Generic<T>() { }
}