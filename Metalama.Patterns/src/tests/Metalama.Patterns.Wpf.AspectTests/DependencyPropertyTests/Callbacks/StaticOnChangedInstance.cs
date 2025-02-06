// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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