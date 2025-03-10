// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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