// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

        var validationResult = this._licenseRegistrationService.ValidateLicenseKey( this.LicenseKey! );

        if ( !validationResult.IsSuccess )
        {
            this.ErrorMessages.Add( validationResult.ErrorMessage );

            return this.Page();
        }
        else
        {
            return this.Redirect( "/Consents" );
        }
    }
}