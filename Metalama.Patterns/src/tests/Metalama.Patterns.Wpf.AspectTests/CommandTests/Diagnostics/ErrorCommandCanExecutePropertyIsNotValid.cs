// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Diagnostics;

public class ErrorCommandCanExecutePropertyIsNotValid
{
    [Command]
    private void ExecuteNotBool() { }

    private int CanExecuteNotBool => 42;

    [Command]
    private void ExecuteNoGetter() { }

    private bool CanExecuteNoGetter
    {
        set { }
    }
}