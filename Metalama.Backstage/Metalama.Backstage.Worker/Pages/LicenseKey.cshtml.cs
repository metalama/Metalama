// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Registration;
using Metalama.Backstage.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Metalama.Backstage.Pages;

#pragma warning disable SA1649
public class LicenseKeyPageModel : PageModel
{
    private readonly ILicenseRegistrationService _licenseRegistrationService;

    public LicenseKeyPageModel( ILicenseRegistrationService licenseRegistrationService )
    {
        this._licenseRegistrationService = licenseRegistrationService;
    }

    [BindProperty]
    [Required]
    public string? LicenseKey
    {
        get => GlobalState.LicenseKey;
        set => GlobalState.LicenseKey = value;
    }

    public List<string> ErrorMessages { get; } = [];

    public IActionResult OnPost()
    {
        if ( !this.ModelState.IsValid )
        {
            this.ErrorMessages.AddRange( this.ModelState.SelectMany( e => e.Value?.Errors ?? Enumerable.Empty<ModelError>() ).Select( e => e.ErrorMessage ) );

            return this.Page();
        }

        if ( !this._licenseRegistrationService.TryValidateLicenseKey( this.LicenseKey!, out var errorMessage ) )
        {
            this.ErrorMessages.Add( errorMessage );

            return this.Page();
        }
        else
        {
            return this.Redirect( "/Consents" );
        }
    }
}