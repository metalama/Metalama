// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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