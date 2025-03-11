// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.ComponentModel;

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.INotifyPropertyChangedIntegration.AnyImplementation;

public class AnyImplementation : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    [Command]
    private void ExecuteFoo1() { }

    public bool CanExecuteFoo1 => true;
}

public class ImplementedByBase : AnyImplementation
{
    [Command]
    private void ExecuteFoo2() { }

    public bool CanExecuteFoo2 => true;
}