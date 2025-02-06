// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Windows;

namespace Metalama.Patterns.Wpf.UnitTests.Assets.DependencyPropertyNS;

public partial class ReadOnlyTestClass : DependencyObject
{
    [DependencyProperty]
    public string Name { get; private set; }

    public void SetName( string name ) => this.Name = name;
}