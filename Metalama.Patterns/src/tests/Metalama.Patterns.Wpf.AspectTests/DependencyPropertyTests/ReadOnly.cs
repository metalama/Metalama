// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Wpf;
using System.Windows;

namespace Metalama.Patterns.Contracts.DependencyPropertyTests.ReadOnly;

public class C : DependencyObject
{
    [DependencyProperty]
    public int TheReadOnlyProperty { get; private set; }

    public void Increment()
    {
        this.TheReadOnlyProperty++;
    }
}