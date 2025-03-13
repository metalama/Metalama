// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.PropertyInitializer.DefaultOptions;

public class DefaultOptions : DependencyObject
{
    private static List<int> InitMethod() => [1, 2, 3];

    [DependencyProperty]
    public List<int> Foo { get; set; } = InitMethod();
}