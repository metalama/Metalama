// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Diagnostics;

public class ErrorCommandCanExecuteMethodIsAmbiguous
{
    [Command]
    private void ExecuteFoo() { }

    private bool CanExecuteFoo() => true;

    private bool CanExecuteFoo( int v ) => true;
}