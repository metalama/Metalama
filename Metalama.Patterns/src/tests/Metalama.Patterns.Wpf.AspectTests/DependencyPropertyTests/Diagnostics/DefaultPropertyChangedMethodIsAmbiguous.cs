// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @RemoveOutputCode
#endif

using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Diagnostics.DefaultPropertyChangedMethodIsAmbiguous;

public class DefaultPropertyChangedMethodIsAmbiguous : DependencyObject
{
    [DependencyProperty]
    public int Foo { get; set; }

    private void OnFooChanged() { }

    private void OnFooChanged( int value ) { }
}