// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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