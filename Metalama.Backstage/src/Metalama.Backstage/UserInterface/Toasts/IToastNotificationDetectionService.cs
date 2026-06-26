// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using System.Threading.Tasks;

namespace Metalama.Backstage.UserInterface.Toasts;

// This service is used in Metalama.Framework.Engine: SourceTransformer's compiler service provider calls DetectAsync()
// once the backstage services and a telemetry context are available for a Metalama-enabled compilation.
[PublicAPI]
public interface IToastNotificationDetectionService : IBackstageService
{
    /// <summary>
    /// Detects and displays any pending toast notifications in the given <paramref name="categories"/>, including
    /// notifications whose detection requires network access (gated on the supplied <paramref name="telemetryContext"/>).
    /// </summary>
    /// <param name="telemetryContext">An optional <see cref="ITelemetryContext"/>, for toast notifications whose detection requires network access.</param>
    /// <param name="categories">The categories of notifications to detect. Defaults to <see cref="ToastNotificationCategories.All"/>.</param>
    /// <remarks>This method should be called by the Metalama framework when an <see cref="ITelemetryContext"/> is known.</remarks>
    Task DetectAsync( ITelemetryContext? telemetryContext = null, ToastNotificationCategories categories = ToastNotificationCategories.All );
}