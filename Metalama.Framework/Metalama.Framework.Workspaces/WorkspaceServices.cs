// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

            BackstageServiceFactoryInitializer.Initialize( new BackstageInitializationOptions( new WorkspaceApplicationInfo() ) );
        }
    }
}