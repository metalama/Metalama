// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.UserInterface;

namespace Metalama.Backstage.Testing;

public sealed class TestUserDeviceDetectionService : IUserDeviceDetectionService
{
    public bool IsInteractiveDevice { get; set; }

    public bool? IsVisualStudioInstalled { get; set; }
}