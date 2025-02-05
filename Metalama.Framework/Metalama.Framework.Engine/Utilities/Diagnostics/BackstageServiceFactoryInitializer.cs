// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Tools;

namespace Metalama.Framework.Engine.Utilities.Diagnostics;

public static class BackstageServiceFactoryInitializer
{
    [PublicAPI]
    public static bool IsInitialized => BackstageServiceFactory.IsInitialized;

    private static BackstageInitializationOptions WithTools( BackstageInitializationOptions options )
        => options with { AddToolsExtractor = builder => builder.AddTools() };

    private static void InitializeMetalamaServices() => Logger.Initialize();

    public static void Initialize( BackstageInitializationOptions options )
    {
        if ( BackstageServiceFactory.Initialize(
                WithTools( options ),
                options.ApplicationInfo.Name ) )
        {
            InitializeMetalamaServices();
        }
    }
}