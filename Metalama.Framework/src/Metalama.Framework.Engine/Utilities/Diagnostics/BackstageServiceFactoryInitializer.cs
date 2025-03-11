// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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