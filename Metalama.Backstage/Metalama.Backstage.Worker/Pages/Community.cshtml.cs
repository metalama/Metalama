// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing;
using Metalama.Backstage.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace Metalama.Backstage.Pages;

#pragma warning disable SA1649

public class CommunityModel : PageModel
{
    [BindProperty]
    public CommunityLicenseReason? UseKind { get; set; }

    public List<string> ErrorMessages { get; } = [];

    public IActionResult OnPost()
    {
        if ( this.UseKind == null )
        {
            this.ErrorMessages.Add( "Please select one of the options above." );

            return this.Page();
        }

        if ( this.UseKind == CommunityLicenseReason.None )
        {
            this.ErrorMessages.Add(
                "We're sorry, but the Metalama Community license is only available for non-commercial use, individuals or small bussinesses. " );

            return this.Page();
        }

        GlobalState.CommunityLicenseReason = this.UseKind.Value;

        return this.Redirect( "/Consents" );
    }
}