// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.UserInterface;
using System;
using System.Collections.Generic;

namespace Metalama.Backstage.Testing;

public class TestUserInterfaceService : UserInterfaceService
{
    public List<ToastNotification> Notifications { get; } = [];

    public TestUserInterfaceService( IServiceProvider serviceProvider ) : base( serviceProvider ) { }

    public override void ShowToastNotification( ToastNotification notification, ref bool notificationReported )
    {
        this.Notifications.Add( notification );
        notificationReported = true;
    }
}