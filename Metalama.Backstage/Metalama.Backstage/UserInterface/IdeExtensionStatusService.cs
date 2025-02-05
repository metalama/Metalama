// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.UserInterface;

internal class IdeExtensionStatusService : IIdeExtensionStatusService
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