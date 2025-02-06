// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// ReSharper disable UnusedMember.Local
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Metalama.Patterns.Wpf.UnitTests.Assets.Command;

public sealed partial class CallbackTestClass : CommandAssetBase
{
    [Command]
    private void ExecuteImplicitInstanceMethodNoParameter()
    {
        LogCall();
    }

    private bool CanExecuteImplicitInstanceMethodNoParameter()
    {
        LogCall();

        return CanExecute();
    }

    [Command]
    private void ExecuteImplicitInstanceMethodWithParameter( int v )
    {
        LogCall( $"{v}" );
    }

    private bool CanExecuteImplicitInstanceMethodWithParameter( int v )
    {
        LogCall( $"{v}" );

        return CanExecute( v );
    }

    [Command]
    private static void ExecuteImplicitStaticMethodNoParameter()
    {
        LogCall();
    }

    private static bool CanExecuteImplicitStaticMethodNoParameter()
    {
        LogCall();

        return CanExecute();
    }

    [Command]
    private static void ExecuteImplicitStaticMethodWithParameter( int v )
    {
        LogCall( $"{v}" );
    }

    private static bool CanExecuteImplicitStaticMethodWithParameter( int v )
    {
        LogCall( $"{v}" );

        return CanExecute( v );
    }

    [Command]
    private void ExecuteImplicitInstanceProperty()
    {
        LogCall();
    }

    private bool CanExecuteImplicitInstanceProperty
    {
        get
        {
            LogCall();

            return CanExecute();
        }
    }

    [Command]
    private static void ExecuteImplicitStaticProperty()
    {
        LogCall();
    }

    private static bool CanExecuteImplicitStaticProperty
    {
        get
        {
            LogCall();

            return CanExecute();
        }
    }
}