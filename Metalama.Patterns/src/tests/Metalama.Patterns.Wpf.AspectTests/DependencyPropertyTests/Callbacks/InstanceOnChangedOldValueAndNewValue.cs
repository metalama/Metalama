// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.InstanceOnChangedOldValueAndNewValue;

public partial class InstanceOnChangedOldValueAndNewValue : DependencyObject
{
    [DependencyProperty]
    public int Foo { get; set; }

    private void OnFooChanged( int oldValue, int newValue ) { }

    [DependencyProperty]
    public List<int> AcceptAssignable { get; set; }

    private void OnAcceptAssignableChanged( IEnumerable<int> oldValue, IEnumerable<int> newValue ) { }

    [DependencyProperty]
    public int AcceptGeneric { get; set; }

    private void OnAcceptGenericChanged<T>( T oldValue, T newValue ) { }

    [DependencyProperty]
    public int AcceptObject { get; set; }

    private void OnAcceptObjectChanged( object oldValue, object newValue ) { }
}