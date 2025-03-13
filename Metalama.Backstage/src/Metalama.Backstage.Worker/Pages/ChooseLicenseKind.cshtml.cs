// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
            case "UseOpenSource":
                GlobalState.SelectedAction = SelectedAction.OpenSource;

                return this.Redirect( "/DoneOpenSource" );

            case "StartTrial":
                GlobalState.SelectedAction = SelectedAction.Trial;

                return this.Redirect( "/Consents" );

            case "Skip":
                GlobalState.SelectedAction = SelectedAction.Skip;

                return this.Redirect( "/Consents" );

            case "RegisterKey":
                GlobalState.SelectedAction = SelectedAction.Register;

                return this.Redirect( "/LicenseKey" );
        }

        return this.Page();
    }
}