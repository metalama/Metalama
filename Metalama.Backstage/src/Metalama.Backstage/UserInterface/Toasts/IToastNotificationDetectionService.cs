// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using System.Threading.Tasks;

namespace Metalama.Backstage.UserInterface.Toasts;

// This service is used in Metalama.Framework.Engine: SourceTransformer.InitializeServices calls Detect()
// once the backstage services are initialized for a Metalama-enabled compilation.
[PublicAPI]
public interface IToastNotificationDetectionService : IBackstageService
{
    /// <summary>
    /// Detect toast notification that
    /// </summary>
    /// <param name="telemetryContext">An optional <see cref="ITelemetryContext"/>, for toast notifications whose detection requires network access.</param>
    Task DetectAsync( ITelemetryContext? telemetryContext = null );
}