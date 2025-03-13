// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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