// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Services;
using Metalama.Backstage.Welcome;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Metalama.Backstage.Pages;

#pragma warning disable SA1649

internal class DonePageModel( WelcomeService welcomeService, RecaptchaService recaptcha ) : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        var isDeviceOnline = await recaptcha.IsDeviceOnlineAsync();

        if ( isDeviceOnline )
        {
            var url = welcomeService.GetWelcomePageUrlAndRemember();

            if ( url != null )
            {
                return this.Redirect( url );
            }
        }
        
        return this.Page();
    }
}