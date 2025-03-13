// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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