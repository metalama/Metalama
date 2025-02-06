// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Diagnostics;

public class ErrorCommandConfiguredCanExecutePropertyNotFound
{
    [Command( CanExecuteProperty = "DoesNotExist" )]
    private void ExecuteFoo() { }
}