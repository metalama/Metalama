// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.ComponentModel;

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.INotifyPropertyChangedIntegration.Disabled;

// When EnableINotifyPropertyChangedIntegration is false, the command must not subscribe to PropertyChanged,
// and the LAMA5202 warning must not be reported for a non-public can-execute property.
public class Disabled : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    [Command( EnableINotifyPropertyChangedIntegration = false )]
    private void ExecuteFoo1() { }

    public bool CanExecuteFoo1 => true;

    [Command( EnableINotifyPropertyChangedIntegration = false )]
    private void ExecuteFoo2() { }

    private bool CanExecuteFoo2 => true;
}
