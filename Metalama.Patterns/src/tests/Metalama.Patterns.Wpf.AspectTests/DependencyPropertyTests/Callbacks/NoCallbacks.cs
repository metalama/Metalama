// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Callbacks.NoCallbacks;

public partial class NoCallbacks : DependencyObject
{
    [DependencyProperty]
    public int Foo { get; set; }
}