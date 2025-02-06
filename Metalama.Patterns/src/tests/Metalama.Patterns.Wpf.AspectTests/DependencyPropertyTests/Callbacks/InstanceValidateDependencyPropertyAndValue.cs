// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.InstanceValidateDependencyPropertyAndValue;

public partial class InstanceValidateDependencyPropertyAndValue : DependencyObject
{
    [DependencyProperty]
    public int Foo { get; set; }

    private void ValidateFoo( DependencyProperty d, int value ) => throw new ArgumentException();

    [DependencyProperty]
    public List<int> AcceptAssignable { get; set; }

    private void ValidateAcceptAssignable( DependencyProperty d, IEnumerable<int> value ) => throw new ArgumentException();

    [DependencyProperty]
    public int AcceptGeneric { get; set; }

    private void ValidateAcceptGeneric<T>( DependencyProperty d, T value ) => throw new ArgumentException();

    [DependencyProperty]
    public int AcceptObject { get; set; }

    private void ValidateAcceptObject( DependencyProperty d, object value ) => throw new ArgumentException();
}