// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.UserInterface;

// This service is intentionally not a part of ProcessUtilities.IsUnattendedProcess to avoid licensing enforcement
// to depend on variable factors like last user input or monitor size.
internal interface IUserDeviceDetectionService : IBackstageService
{
    bool IsInteractiveDevice { get; }

    bool? IsVisualStudioInstalled { get; }
}