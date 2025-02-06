// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.StaticValidateInstanceAndValue;

public partial class StaticValidateInstanceAndValue : DependencyObject
{
    [DependencyProperty]
    public int Foo { get; set; }

    private static void ValidateFoo( StaticValidateInstanceAndValue instance, int value ) { }

    [DependencyProperty]
    public List<int> AcceptsAssignableForValue { get; set; }

    private static void ValidateAcceptsAssignableForValue( StaticValidateInstanceAndValue instance, IEnumerable<int> value ) { }

    [DependencyProperty]
    public int AcceptsGenericForValue { get; set; }

    private static void ValidateAcceptsGenericForValue<T>( StaticValidateInstanceAndValue instance, T value ) { }

    [DependencyProperty]
    public int AcceptsObjectForValue { get; set; }

    private static void ValidateAcceptsObjectForValue( StaticValidateInstanceAndValue instance, object value ) { }

    [DependencyProperty]
    public int AcceptsDependencyObjectForInstance { get; set; }

    private static void ValidateAcceptsDependencyObjectForInstance( DependencyObject instance, int value ) { }

    [DependencyProperty]
    public int AcceptsObjectForInstance { get; set; }

    private static void ValidateAcceptsObjectForInstance( object instance, int value ) { }
}