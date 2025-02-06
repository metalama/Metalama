// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.StaticValidateDependencyPropertyAndInstanceAndValue;

public partial class StaticValidateDependencyPropertyAndInstanceAndValue : DependencyObject
{
    [DependencyProperty]
    public int Foo { get; set; }

    private static void ValidateFoo( DependencyProperty d, StaticValidateDependencyPropertyAndInstanceAndValue instance, int value ) { }

    [DependencyProperty]
    public List<int> AcceptsAssignableForValue { get; set; }

    private static void ValidateAcceptsAssignableForValue(
        DependencyProperty d,
        StaticValidateDependencyPropertyAndInstanceAndValue instance,
        IEnumerable<int> value ) { }

    [DependencyProperty]
    public int AcceptsGenericForValue { get; set; }

    private static void ValidateAcceptsGenericForValue<T>( DependencyProperty d, StaticValidateDependencyPropertyAndInstanceAndValue instance, T value ) { }

    [DependencyProperty]
    public int AcceptsObjectForValue { get; set; }

    private static void ValidateAcceptsObjectForValue( DependencyProperty d, StaticValidateDependencyPropertyAndInstanceAndValue instance, object value ) { }

    [DependencyProperty]
    public int AcceptsDependencyObjectForInstance { get; set; }

    private static void ValidateAcceptsDependencyObjectForInstance( DependencyProperty d, DependencyObject instance, int value ) { }

    [DependencyProperty]
    public int AcceptsObjectForInstance { get; set; }

    private static void ValidateAcceptsObjectForInstance( DependencyProperty d, object instance, int value ) { }
}