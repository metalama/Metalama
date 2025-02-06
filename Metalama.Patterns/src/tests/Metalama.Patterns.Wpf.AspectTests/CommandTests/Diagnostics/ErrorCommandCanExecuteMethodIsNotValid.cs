// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Diagnostics;

public class ErrorCommandCanExecuteMethodIsNotValid
{
    [Command]
    private void ExecuteGeneric() { }

    private bool CanExecuteGeneric<T>( T value ) => true;

    [Command]
    private void ExecuteNotBool() { }

    private int CanExecuteNotBool() => 42;

    [Command]
    private void ExecuteTwoParameters() { }

    private bool CanExecuteTwoParameters( int a, int b ) => true;

    [Command]
    private void ExecuteRefParameter() { }

    private bool CanExecuteRefParameter( ref int a ) => true;
}