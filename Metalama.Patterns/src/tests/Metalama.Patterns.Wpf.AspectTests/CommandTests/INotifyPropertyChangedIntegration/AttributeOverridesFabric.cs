// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Fabrics;
using Metalama.Patterns.Wpf.Configuration;
using System.ComponentModel;

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.INotifyPropertyChangedIntegration.AttributeOverridesFabric;

internal class NsFabric : NamespaceFabric
{
    public override void AmendNamespace( INamespaceAmender amender )
    {
        amender.ConfigureCommand( b => b.EnableINotifyPropertyChangedIntegration = false );
    }
}

// <target>
// The fabric disables INotifyPropertyChanged integration. Foo1 inherits this setting, and Foo2 enables it again
// through the attribute, which takes precedence over the fabric.
public class AttributeOverridesFabric : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    [Command]
    private void ExecuteFoo1() { }

    public bool CanExecuteFoo1 => true;

    [Command( EnableINotifyPropertyChangedIntegration = true )]
    private void ExecuteFoo2() { }

    public bool CanExecuteFoo2 => true;
}
