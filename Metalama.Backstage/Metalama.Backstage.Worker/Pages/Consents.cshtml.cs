// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Licensing.Registration;
using Metalama.Backstage.Pages.Shared;
using Metalama.Backstage.Services;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Telemetry.User;
using Metalama.Backstage.UserInterface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IHttpClientFactory = Metalama.Backstage.Infrastructure.IHttpClientFactory;

namespace Metalama.Backstage.Pages;

#pragma warning disable SA1649

internal class ConsentsPageModel : PageModel
{
    private readonly ILicenseRegistrationService _licenseRegistrationService;
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;
    private readonly IIdeExtensionStatusService? _ideExtensionStatusService;
    private readonly IToastNotificationStatusService _toastNotificationStatusService;
    private readonly WebLinks _webLinks;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApplicationInfo _applicationInfo;
    private readonly RecaptchaService _recaptcha;
    private readonly IUserInfoService _userInfoService;

    public ConsentsPageModel(
        ILicenseRegistrationService licenseRegistrationService,
        ITelemetryConfigurationService telemetryConfigurationService,
        IToastNotificationStatusService toastNotificationStatusService,
        WebLinks webLinks,
        IHttpClientFactory httpClientFactory,
        IApplicationInfoProvider applicationInfoProvider,
        IUserInfoService userInfoService,
        RecaptchaService recaptcha,
        IIdeExtensionStatusService? ideExtensionStatusService = null )
    {
        this._licenseRegistrationService = licenseRegistrationService;
        this._telemetryConfigurationService = telemetryConfigurationService;
        this._ideExtensionStatusService = ideExtensionStatusService;
        this._webLinks = webLinks;
        this._httpClientFactory = httpClientFactory;
        this._toastNotificationStatusService = toastNotificationStatusService;
        this._applicationInfo = applicationInfoProvider.CurrentApplication;
        this._recaptcha = recaptcha;
        this._userInfoService = userInfoService;
    }

    public List<string> ErrorMessages { get; } = [];

    public string RecaptchaSiteKey { get; set; } = "<not set>";

    [BindProperty]
    public bool EnableTelemetry { get; set; }

    [BindProperty]
    [EmailAddress]
    [DisplayName( "Email" )]
    public string? EmailAddress { get; set; }

    [BindProperty]
    public bool SubscribeToNewsletter { get; set; }

    [BindProperty]
    public bool AcceptLicense { get; set; }

    [BindProperty]
    public string? RecaptchaResponse { get; set; }

    public bool IsDeviceOnline { get; set; }

    private async Task PrepareCaptchaAsync()
    {
        var recaptchaSiteKey = await this._recaptcha.GetRecaptchaSiteKeyAsync();
        
        if ( recaptchaSiteKey != null )
        {
            this.RecaptchaSiteKey = recaptchaSiteKey;
            this.IsDeviceOnline = true;
        }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await this.PrepareCaptchaAsync();

        // If newsletter is not available, we keep the email address empty, so the field validation is not triggered.
        if ( this.IsDeviceOnline )
        {
            this.EmailAddress = this._userInfoService.TryGetUserInfo( out var i ) ? i.EmailAddress : null;
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await this.PrepareCaptchaAsync();

        if ( !this.ModelState.IsValid )
        {
            this.ErrorMessages.AddRange( this.ModelState.SelectMany( e => e.Value?.Errors ?? Enumerable.Empty<ModelError>() ).Select( e => e.ErrorMessage ) );

            return this.Page();
        }

        if ( !this.AcceptLicense )
        {
            this.ErrorMessages.Add( "You must accept the license agreement." );

            return this.Page();
        }

        if ( this.SubscribeToNewsletter )
        {
            // If the value is not null or empty, it gets validated by the web framework.
            if ( string.IsNullOrEmpty( this.EmailAddress ) )
            {
                this.ErrorMessages.Add( "The email address is required to subscribe to the newsletter." );
                
                return this.Page();
            }
            
            this._userInfoService.SaveEmailAddress( this.EmailAddress );
            
            if ( string.IsNullOrEmpty( this.RecaptchaResponse ) )
            {
                this.ErrorMessages.Add( "The reCaptcha challenge was invalid." );

                return this.Page();
            }

            var httpClient = this._httpClientFactory.Create();

            try
            {
                await httpClient.GetStringAsync(
                    this._webLinks.NewsletterSubscribeApi
                    + $"?captcha={WebUtility.UrlEncode( this.RecaptchaResponse )}"
                    + $"&version={this._applicationInfo.PackageVersion}"
                    + $"&email={WebUtility.UrlEncode( this.EmailAddress )}"
                    + $"&registration={GlobalState.LicenseKind.ToString().ToLowerInvariant()}" );
            }
            catch ( Exception e )
            {
                this.ErrorMessages.Add( $"Cannot subscribe to the newsletter: {e.Message}" );

                return this.Page();
            }
        }

        // Register the license.
        switch ( GlobalState.LicenseKind )
        {
            case LicenseKind.Trial:
                {
                    if ( !this._licenseRegistrationService.TryRegisterTrialEdition( out var errorMessage ) )
                    {
                        this.ErrorMessages.Add( errorMessage );

                        return this.Page();
                    }

                    break;
                }

            case LicenseKind.Free:
                {
                    if ( !this._licenseRegistrationService.TryRegisterFreeEdition( out var errorMessage ) )
                    {
                        this.ErrorMessages.Add( errorMessage );

                        return this.Page();
                    }

                    break;
                }

            case LicenseKind.Skip:
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

                    if ( !this._licenseRegistrationService.TryRegisterLicense( GlobalState.LicenseKey, out var errorMessage ) )
                    {
                        this.ErrorMessages.Add( errorMessage );

                        return this.Page();
                    }

                    break;
                }
        }

        // Configure telemetry.
        this._telemetryConfigurationService.SetStatus( this.EnableTelemetry );

        // Should we recommend to install Visual Studio
        if ( this._ideExtensionStatusService?.ShouldRecommendToInstallVisualStudioExtension == true )
        {
            return this.Redirect( "/InstallVsx" );
        }

        return this.Redirect( "/Done" );
    }
}