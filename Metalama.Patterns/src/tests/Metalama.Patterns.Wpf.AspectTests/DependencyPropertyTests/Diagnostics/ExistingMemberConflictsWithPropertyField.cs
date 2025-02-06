// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Diagnostics.ExistingMemberConflictsWithPropertyField;

public class ExistingMemberConflictsWithPropertyField : DependencyObject
{
    [DependencyProperty]
    public int Foo { get; set; }

    private void FooProperty() { }
}