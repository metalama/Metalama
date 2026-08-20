// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Testing;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.UserInterface.Toasts;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.UserInterface;

/// <summary>
/// Verifies that a failure to display a toast notification does not propagate to the caller. On a continuous
/// integration agent, the Backstage Worker tool cannot be started, so the attempt to open the configuration web page
/// throws. That failure used to escape the licensing verification task and to be reported as MSB4018. See issue #1859.
/// </summary>
public sealed class BrowserBasedUserInterfaceServiceTests : TestsBase
{
    public BrowserBasedUserInterfaceServiceTests( ITestOutputHelper logger ) : base( logger ) { }

    [Fact]
    public void ShowToastNotificationDoesNotThrowWhenTheWorkerToolCannotBeStarted()
    {
        this.UserDeviceDetection.IsInteractiveDevice = true;
        this.ProcessExecutor.ExceptionToThrow = new FileNotFoundException( "The file 'Metalama.Backstage.Worker.dll' does not exist." );

        var service = new BrowserBasedUserInterfaceService( this.ServiceProvider );

        service.ShowToastNotification( new ToastNotification( ToastNotificationKinds.RequiresLicense ) );
    }
}
