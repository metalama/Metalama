// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.UserInterface.Rss;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel;
using IConfigurationManager = Metalama.Backstage.Configuration.IConfigurationManager;

namespace Metalama.Backstage.Pages;

#pragma warning disable SA1649

internal class RssOptionsPageModel : PageModel
{
    private readonly IConfigurationManager _configurationManager;

    public RssOptionsPageModel( IConfigurationManager configurationManager )
    {
        this._configurationManager = configurationManager;
    }

    [BindProperty]
    [DisplayName( "Preferred Feed" )]
    public RssFeed? PreferredFeed { get; set; }

    public void OnGet()
    {
        // Load the current preference
        this.PreferredFeed = this._configurationManager.Get<RssClientConfiguration>()?.PreferredFeed ?? RssFeed.Briefs;
    }

    public IActionResult OnPost()
    {
        if ( !this.ModelState.IsValid )
        {
            return this.Page();
        }

        // Save the preference
        this._configurationManager.Update<RssClientConfiguration>( c => c with { PreferredFeed = this.PreferredFeed } );

        return this.Page();
    }
}