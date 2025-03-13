// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.StaticOnChangedDependencyPropertyAndInstance;

public partial class StaticOnChangedDependencyPropertyAndInstance : DependencyObject
{
    [DependencyProperty]
    public int Foo { get; set; }

    private static void OnFooChanged( DependencyProperty d, StaticOnChangedDependencyPropertyAndInstance instance ) { }

    [DependencyProperty]
    public int AcceptsDependencyObjectForInstance { get; set; }

    private static void OnAcceptsDependencyObjectForInstanceChanged( DependencyProperty d, DependencyObject instance ) { }

    [DependencyProperty]
    public int AcceptsObjectForInstance { get; set; }

    private static void OnAcceptsObjectForInstanceChanged( DependencyProperty d, object instance ) { }
}