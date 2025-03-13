// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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