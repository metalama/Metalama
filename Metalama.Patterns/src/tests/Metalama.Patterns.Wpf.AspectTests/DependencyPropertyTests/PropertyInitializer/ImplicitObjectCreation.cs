// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.PropertyInitializer.ImplicitObjectCreation;

public class ImplicitObjectCreation : DependencyObject
{
    [DependencyProperty]
    public List<int> Foo { get; set; } = [1, 2, 3];
}