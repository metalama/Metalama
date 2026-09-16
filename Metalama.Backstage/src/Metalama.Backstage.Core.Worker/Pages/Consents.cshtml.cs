// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Registration;
using Metalama.Backstage.Pages.Shared;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.UserInterface.Toasts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace Metalama.Backstage.Pages;

#pragma warning disable SA1649

internal class ConsentsPageModel : PageModel
{
    private readonly ILicenseRegistrationService _licenseRegistrationService;
    private readonly IIdeExtensionStatusService? _ideExtensionStatusService;
    private readonly IToastNotificationStatusService _toastNotificationStatusService;

    public ConsentsPageModel(
        ILicenseRegistrationService licenseRegistrationService,
        IToastNotificationStatusService toastNotificationStatusService,
        IIdeExtensionStatusService? ideExtensionStatusService = null )
    {
        this._licenseRegistrationService = licenseRegistrationService;
        this._ideExtensionStatusService = ideExtensionStatusService;
        this._toastNotificationStatusService = toastNotificationStatusService;
    }

    public List<string> ErrorMessages { get; } = [];

    [BindProperty]
    public bool AcceptLicense { get; set; }

    public string BackUrl { get; private set; } = "/ChooseLicenseKind";

    public IActionResult OnGet()
    {
        this.BackUrl = GlobalState.SelectedAction == SelectedAction.Register ? "/LicenseKey" : "/ChooseLicenseKind";

        return this.Page();
    }

    public IActionResult OnPost()
    {
        this.BackUrl = GlobalState.SelectedAction == SelectedAction.Register ? "/LicenseKey" : "/ChooseLicenseKind";

        if ( !this.AcceptLicense )
        {
            this.ErrorMessages.Add( "You must accept the license agreement." );

            return this.Page();
        }

        // Register the license.
        switch ( GlobalState.SelectedAction )
        {
            case SelectedAction.OpenSource:
                return this.Redirect( "/DoneOpenSource" );

            case SelectedAction.Trial:
                {
                    if ( !ProcessRegistrationResult( this._licenseRegistrationService.RegisterTrialEdition() ) )
                    {
                        return this.Page();
                    }

                    break;
                }

            case SelectedAction.Skip:
                {
                    this._toastNotificationStatusService.Mute( ToastNotificationKinds.RequiresLicense );

                    break;
                }

            default:
                {
                    if ( GlobalState.LicenseKey == null )
                    {
                        this.ErrorMessages.Add( "No license key was provided." );

                        return this.Page();
                    }

                    if ( !ProcessRegistrationResult( this._licenseRegistrationService.RegisterLicense( GlobalState.LicenseKey ) ) )
                    {
                        return this.Page();
                    }

                    break;
                }
        }

        // Should we recommend to install Visual Studio
        if ( this._ideExtensionStatusService?.ShouldRecommendToInstallVisualStudioExtension == true )
        {
            return this.Redirect( "/InstallVsx" );
        }

        return this.Redirect( "/Done" );

        bool ProcessRegistrationResult( LicenseRegistrationResult result )
        {
            if ( result.IsSuccess )
            {
                return true;
            }
            else
            {
                this.ErrorMessages.Add( result.ErrorMessage );

                return false;
            }
        }
    }
}
