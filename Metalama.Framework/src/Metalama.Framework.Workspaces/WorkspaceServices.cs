// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Framework.Engine.Utilities.Diagnostics;

namespace Metalama.Framework.Workspaces;

internal static class WorkspaceServices
{
    public static void Initialize()
    {
        // We don't initialize if another process has initialized.

        if ( !BackstageServiceFactoryInitializer.IsInitialized )
        {
            // Don't enforce licensing in workspaces.

            BackstageServiceFactoryInitializer.Initialize( new BackstageInitializationOptions( new WorkspaceApplicationInfo() ) { AddLicensing = false } );
        }
    }
}