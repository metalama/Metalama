// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Callbacks;

public class ExplicitlyConfiguredByCommandAttribute
{
    [Command( CanExecuteMethod = nameof(SomeWeirdName1) )]
    private void Exec1() { }

    private bool SomeWeirdName1() => true;

    [Command( CanExecuteMethod = nameof(CanExec1) )]
    private void ExecuteConfiguredCanExecuteMethod() { }

    // Has the default can-execute name for Exec1() above, don't be fooled.
    private bool CanExec1() => true;

    [Command( CanExecuteProperty = nameof(CanExec2) )]
    private void ExecuteConfiguredCanExecuteProperty() { }

    private bool CanExec2 => true;
}