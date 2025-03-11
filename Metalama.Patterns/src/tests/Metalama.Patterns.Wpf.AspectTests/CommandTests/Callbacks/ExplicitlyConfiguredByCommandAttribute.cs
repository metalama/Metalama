// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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