// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Pages.Shared;
using Metalama.Backstage.UserInterface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Metalama.Backstage.Pages;

#pragma warning disable SA1649

public class ChooseLicenseKindPageModel : PageModel
{
    public ChooseLicenseKindPageModel( WebLinks webLinks )
    {
        this.WebLinks = webLinks;
    }

    public WebLinks WebLinks { get; }

    public IActionResult OnPost( string action )
    {
        switch ( action )
        {
            case "StartFree":
                GlobalState.LicenseKind = LicenseKind.Free;

                return this.Redirect( "/Consents" );

            case "StartTrial":
                GlobalState.LicenseKind = LicenseKind.Trial;

                return this.Redirect( "/Consents" );

            case "Skip":
                GlobalState.LicenseKind = LicenseKind.Skip;

                return this.Redirect( "/Consents" );

            case "RegisterKey":
                GlobalState.LicenseKind = LicenseKind.Register;

                return this.Redirect( "/LicenseKey" );
        }

        return this.Page();
    }
}