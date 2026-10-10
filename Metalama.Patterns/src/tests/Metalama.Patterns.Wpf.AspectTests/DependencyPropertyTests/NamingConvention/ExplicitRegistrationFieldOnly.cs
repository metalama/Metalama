// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.NamingConvention.ExplicitRegistrationFieldOnly;

// <target>
public class ExplicitRegistrationFieldOnly : DependencyObject
{
    // The explicit name must be used even when no callback method is specified.
    [DependencyProperty( RegistrationField = "TheFooProperty" )]
    public int Foo { get; set; }

    // Found by the default callback name, which still applies.
    private void OnFooChanged() { }

    // Conflicts with the name determined by the default naming convention.
    private void FooProperty() { }
}
