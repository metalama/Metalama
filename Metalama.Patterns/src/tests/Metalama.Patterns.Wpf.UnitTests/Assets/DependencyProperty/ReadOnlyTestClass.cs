// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Windows;

namespace Metalama.Patterns.Wpf.UnitTests.Assets.DependencyPropertyNS;

public partial class ReadOnlyTestClass : DependencyObject
{
    [DependencyProperty]
    public string Name { get; private set; }

    public void SetName( string name ) => this.Name = name;
}