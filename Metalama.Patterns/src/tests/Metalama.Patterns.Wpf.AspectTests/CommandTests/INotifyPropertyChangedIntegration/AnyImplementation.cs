// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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