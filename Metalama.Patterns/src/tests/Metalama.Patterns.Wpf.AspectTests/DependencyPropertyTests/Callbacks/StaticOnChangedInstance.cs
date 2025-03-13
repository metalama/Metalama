// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.StaticOnChangedInstance;

public partial class StaticOnChangedInstance : DependencyObject
{
    [DependencyProperty]
    public int Foo { get; set; }

    private static void OnFooChanged( StaticOnChangedInstance instance ) { }

    [DependencyProperty]
    public int AcceptsDependencyObject { get; set; }

    private static void OnAcceptsDependencyObjectChanged( DependencyObject instance ) { }

    [DependencyProperty]
    public int AcceptsObject { get; set; }

    private static void OnAcceptsObjectChanged( object instance ) { }
}