// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.UserInterface;
using System;
using System.Threading.Tasks;

namespace Metalama.Backstage.Desktop.Windows.Commands;

// Opens the exception-report review page (formatted report + Report button + per-category auto-report checkbox) using
// the worker-process web server. Activated when the user clicks the exception toast. See #1674.
[UsedImplicitly( ImplicitUseTargetFlags.WithMembers )]
internal sealed class OpenExceptionReportCommand : BaseAsyncCommand<OpenExceptionReportCommandSettings>
{
    public const string Name = "exception-report";

    protected override async Task<int> ExecuteAsync( ExtendedCommandContext context, OpenExceptionReportCommandSettings settings )
    {
        var serviceProvider = App.GetBackstageServices( settings );
        var userInterfaceService = serviceProvider.GetRequiredBackstageService<IUserInterfaceService>();

        // The command receives only the report id and builds the review-page path itself, so it is specifically about
        // opening an exception report rather than an arbitrary URL. See #1674.
        var page = "ExceptionReport?report=" + Uri.EscapeDataString( settings.Report );

        await userInterfaceService.OpenConfigurationWebPageAsync( page );

        return 0;
    }
}
