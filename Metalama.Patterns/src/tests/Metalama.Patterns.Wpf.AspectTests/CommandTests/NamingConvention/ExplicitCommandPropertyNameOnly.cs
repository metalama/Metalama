// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.NamingConvention.ExplicitCommandPropertyNameOnly;

// <target>
public class ExplicitCommandPropertyNameOnly
{
    // The explicit name must be used even when no CanExecute member is specified.
    [Command( CommandPropertyName = "TheFooCommand" )]
    private void ExecuteFoo() { }

    // Conflicts with the name determined by the default naming convention.
    private int FooCommand;
}
