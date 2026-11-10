// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage;
using Metalama.Compiler;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using SharpCrafters.Backstage.Extensibility;

namespace Metalama.Framework.DesignTime;

internal static class DesignTimeServices
{
    public static void Initialize()
    {
        if ( MetalamaCompilerInfo.IsActive )
        {
            throw new InvalidOperationException( "This method cannot be called from the Metalama Compiler process." );
        }

        var isTestHost = DesignTimeHostOverride.IsVisualStudioInProcess;

        if ( isTestHost )
        {
            DisableTelemetry();
        }

        BackstageServiceFactoryInitializer.Initialize(
            new BackstageInitializationOptions( new MetalamaDesignTimeApplicationInfo(), MetalamaProduct.Instance )
            {
                AddSupportServices = true,

                // A test process must not show a user interface, for instance a toast notification.
                AddUserInterface = !isTestHost,
                AddLicensing = false
            } );
    }

    /// <summary>
    /// Disables telemetry in a process that hosts both Visual Studio processes for a test.
    /// </summary>
    /// <remarks>
    /// The options of Backstage that control the collection and the upload of telemetry are not public, so the process
    /// sets the environment variable that Backstage reads. A value that the caller has already set is left unchanged.
    /// </remarks>
    private static void DisableTelemetry()
    {
        const string telemetryOptOutVariableName = "METALAMA_TELEMETRY_OPT_OUT";

        if ( string.IsNullOrEmpty( Environment.GetEnvironmentVariable( telemetryOptOutVariableName ) ) )
        {
            Environment.SetEnvironmentVariable( telemetryOptOutVariableName, "1" );
        }
    }
}