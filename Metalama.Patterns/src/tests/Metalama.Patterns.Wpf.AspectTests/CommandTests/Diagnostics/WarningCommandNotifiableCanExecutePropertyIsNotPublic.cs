// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @RemoveOutputCode
#endif

using System.ComponentModel;

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Diagnostics;

public class WarningCommandNotifiableCanExecutePropertyIsNotPublic : INotifyPropertyChanged
{
    [Command]
    private void ExecutePrivateCanExecute() { }

    private bool CanExecutePrivateCanExecute => true;

    public event PropertyChangedEventHandler? PropertyChanged;
}