// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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