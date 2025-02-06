// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.InstanceOnChangedValue;

public partial class InstanceOnChangedValue : DependencyObject
{
    [DependencyProperty]
    public int Foo { get; set; }

    private void OnFooChanged( int value ) { }

    [DependencyProperty]
    public List<int> AcceptAssignable { get; set; }

    private void OnAcceptAssignableChanged( IEnumerable<int> value ) { }

    [DependencyProperty]
    public int AcceptGeneric { get; set; }

    private void OnAcceptGenericChanged<T>( T value ) { }

    [DependencyProperty]
    public int AcceptObject { get; set; }

    private void OnAcceptObjectChanged( object value ) { }
}