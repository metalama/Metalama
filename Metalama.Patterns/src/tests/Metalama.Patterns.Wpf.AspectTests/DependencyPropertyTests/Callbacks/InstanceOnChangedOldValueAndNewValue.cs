// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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