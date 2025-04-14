// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.UserInterface;
using System;

namespace Metalama.Backstage.Extensibility;

/// <summary>
/// Initialization options for the <see cref="RegisterServiceExtensions.AddBackstageServices"/> method.
/// </summary>
/// <param name="ApplicationInfo">The <see cref="IApplicationInfo"/> of the caller.</param>
[PublicAPI]
public record BackstageInitializationOptions( IApplicationInfo ApplicationInfo )
{
    /// <summary>
    /// Gets the full path of the .NET SDK directory of the current process.
    /// </summary>
    public string? DotNetSdkDirectory { get; init; }

    /// <summary>
    /// Gets a value indicating whether logging and telemetry services should be registered.
    /// </summary>
    public bool AddSupportServices { get; init; }

    public bool AddDumperService { get; init; }

    /// <summary>
    /// Gets a value indicating whether licensing services should be registered.
    /// </summary>
    public bool AddLicensing { get; init; }

    public bool AddUserInterface { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current program executes from a development environment,
    /// i.e. tools are located under the bin directory of their respective projects. 
    /// </summary>
    public bool IsDevelopmentEnvironment { get; init; }

    public Action<ServiceProviderBuilder>? AddToolsExtractor { get; init; }

    /// <summary>
    /// Gets the licensing options, when <see cref="AddLicensing"/> is <c>true</c>.
    /// </summary>
    public LicensingInitializationOptions LicensingOptions { get; init; } = new();

    /// <summary>
    /// Gets a value indicating whether the services should be initialized. The default value is <c>true</c>.
    /// It can be set to <c>false</c> in scenarios where it is not necessary to build up the whole application
    /// because just a few services will be used.
    /// </summary>
    public bool Initialize { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether toast notifications like <see cref="ToastNotificationKinds.RequiresLicense"/> or
    /// <see cref="ToastNotificationKinds.VsxNotInstalled"/> should be detected and opened. The default value is <c>true</c>.
    /// </summary>
    public bool DetectToastNotifications { get; init; } = true;

    /// <summary>
    /// Gets diagnostic (tracing) options. Considered only when <see cref="AddSupportServices"/> is <c>true</c>. 
    /// </summary>
    public DiagnosticsInitializationOptions DiagnosticsOptions { get; init; } = new();

    internal bool AutoUploadTelemetry { get; init; } = true;
}