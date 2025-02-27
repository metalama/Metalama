// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.UserInterface;

// This service is used in Metalama.Framework.Engine.
// The detection is not called when a project has no aspects or validators.
public interface IToastNotificationDetectionService : IBackstageService
{
    void Detect();
}