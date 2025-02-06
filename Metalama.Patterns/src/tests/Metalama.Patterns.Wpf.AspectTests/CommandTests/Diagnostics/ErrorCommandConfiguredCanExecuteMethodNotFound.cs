// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Diagnostics;

public class ErrorCommandConfiguredCanExecuteMethodNotFound
{
    [Command( CanExecuteMethod = "DoesNotExist" )]
    private void ExecuteFoo() { }
}