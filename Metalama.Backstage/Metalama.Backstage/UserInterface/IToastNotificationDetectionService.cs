// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.UserInterface;

// This service is used in Metalama.Framework.Engine.
// The detection is not called when a project has no aspects or validators.
public interface IToastNotificationDetectionService : IBackstageService
{
    void Detect();
}