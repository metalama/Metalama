// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.InstanceValidateValue;

public partial class InstanceValidateValue : DependencyObject
{
    [DependencyProperty]
    public int Foo { get; set; }

    private void ValidateFoo( int value ) => throw new ArgumentException();

    [DependencyProperty]
    public List<int> AcceptAssignable { get; set; }

    private void ValidateAcceptAssignable( IEnumerable<int> value ) => throw new ArgumentException();

    [DependencyProperty]
    public int AcceptGeneric { get; set; }

    private void ValidateAcceptGeneric<T>( T value ) => throw new ArgumentException();

    [DependencyProperty]
    public int AcceptObject { get; set; }

    private void ValidateAcceptObject( object value ) => throw new ArgumentException();
}