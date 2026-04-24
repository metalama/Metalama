// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Extensibility;
using Metalama.Framework.DesignTime.Services;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.EndToEnd;

/// <summary>
/// Test design-time extension that adds an RPC service when initialized.
/// This extension is used to reproduce GitHub issue #1242.
/// </summary>
public sealed class TestDesignTimeExtension : IDesignTimeExtension
{
    public const string ExtensionName = "TestDesignTimeExtension";

    public string Name => ExtensionName;

    public bool Initialize( DesignTimeInitializationContext context )
    {
        // Configure shared services like Premium's CodeFixesDesignTimeExtension does.
        // This might trigger earlier service resolution.
        context.ConfigureSharedServices(
            serviceProvider => serviceProvider.Underlying
                .AddSharedService( _ => new TestSharedService() ) );

        // Only add RPC service in the analysis process (server side)
        if ( context.ProcessKind == DesignTimeProcessKind.VsAnalysisProcess )
        {
            context.AddRpcService( new TestExtensionServiceFactory() );
        }

        return true;
    }
}