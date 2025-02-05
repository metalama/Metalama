// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

#pragma warning disable SA1649

namespace Metalama.Backstage.Pages
{
    [ResponseCache( Duration = 0, Location = ResponseCacheLocation.None, NoStore = true )]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }

        // ReSharper disable once UnusedMember.Global
        public bool ShowRequestId => !string.IsNullOrEmpty( this.RequestId );

        public void OnGet()
        {
            this.RequestId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier;
        }
    }
}