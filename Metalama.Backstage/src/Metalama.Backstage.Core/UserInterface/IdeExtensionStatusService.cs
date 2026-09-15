// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.UserInterface;

internal sealed class IdeExtensionStatusService : IIdeExtensionStatusService
{
    private readonly IUserDeviceDetectionService _userDeviceDetectionService;
    private readonly IConfigurationManager _configurationManager;

    public IdeExtensionStatusService( IServiceProvider serviceProvider )
    {
        this._userDeviceDetectionService = serviceProvider.GetRequiredBackstageService<IUserDeviceDetectionService>();
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
    }

    public bool ShouldRecommendToInstallVisualStudioExtension
        => this._userDeviceDetectionService is { IsInteractiveDevice: true, IsVisualStudioInstalled: true }
           && !this.IsVisualStudioExtensionInstalled;

    public bool IsVisualStudioExtensionInstalled
    {
        get => this._configurationManager.Get<IdeExtensionsStatusConfiguration>().IsVisualStudioExtensionInstalled;
        set => this._configurationManager.Update<IdeExtensionsStatusConfiguration>( c => c with { IsVisualStudioExtensionInstalled = value } );
    }
}