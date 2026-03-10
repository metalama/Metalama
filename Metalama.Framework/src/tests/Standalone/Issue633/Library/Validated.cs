// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Contracts;

namespace Issue633.Library;

// Regression test for #633: A library applies contract attributes and uses PrivateAssets="all"
// on Metalama.Patterns.Contracts so the package doesn't flow to consumers.
public class Validated
{
    public string GetGreeting( [NotNull] string name )
    {
        return $"Hello, {name}!";
    }

    public int Clamp( [Range( 0, 100 )] int value )
    {
        return value;
    }
}
