// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.StaticValidateDependencyPropertyAndValue;

public partial class StaticValidateDependencyPropertyAndValue : DependencyObject
{
    [DependencyProperty]
    public int Foo { get; set; }

    private static void ValidateFoo( DependencyProperty d, int value ) { }

    [DependencyProperty]
    public List<int> AcceptsAssignable { get; set; }

    private static void ValidateAcceptsAssignable( DependencyProperty d, IEnumerable<int> value ) { }

    [DependencyProperty]
    public int AcceptsGeneric { get; set; }

    private static void ValidateAcceptsGeneric<T>( DependencyProperty d, T value ) { }

    [DependencyProperty]
    public int AcceptsObject { get; set; }

    private static void ValidateAcceptsObject( DependencyProperty d, object value ) { }
}