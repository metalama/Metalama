// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Metalama.Backstage.Pages;

#pragma warning disable SA1649

public class SetupPageModel : PageModel
{
    public IActionResult OnGet()
    {
        return this.Redirect( "/ChooseLicenseKind" );
    }
}