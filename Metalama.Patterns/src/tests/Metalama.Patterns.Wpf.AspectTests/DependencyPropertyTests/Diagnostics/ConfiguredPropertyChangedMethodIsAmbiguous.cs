// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RemoveOutputCode
#endif

using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Diagnostics.ConfiguredPropertyChangedMethodIsAmbiguous;

public class ConfiguredPropertyChangedMethodIsAmbiguous : DependencyObject
{
    [DependencyProperty( PropertyChangedMethod = nameof(Changed) )]
    public int Foo { get; set; }

    private void Changed() { }

    private void Changed( int value ) { }
}