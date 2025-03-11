// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.UserInterface;

// This service is intentionally not a part of ProcessUtilities.IsUnattendedProcess to avoid licensing enforcement
// to depend on variable factors like last user input or monitor size.
internal interface IUserDeviceDetectionService : IBackstageService
{
    bool IsInteractiveDevice { get; }

    bool? IsVisualStudioInstalled { get; }
}