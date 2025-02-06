// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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