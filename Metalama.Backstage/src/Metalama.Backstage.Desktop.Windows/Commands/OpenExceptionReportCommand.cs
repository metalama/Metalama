// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.UserInterface;
using System.Threading.Tasks;

namespace Metalama.Backstage.Desktop.Windows.Commands;

// Opens the exception-report review page (formatted report + Report button + per-category auto-report checkbox) using the
// worker-process web server. Activated when the user clicks the exception toast. See #1674.
[UsedImplicitly( ImplicitUseTargetFlags.WithMembers )]
internal sealed class OpenExceptionReportCommand : BaseAsyncCommand<OpenExceptionReportCommandSettings>
{
    public const string Name = "exception-report";

    protected override async Task<int> ExecuteAsync( ExtendedCommandContext context, OpenExceptionReportCommandSettings settings )
    {
        var serviceProvider = App.GetBackstageServices( settings );
        var userInterfaceService = serviceProvider.GetRequiredBackstageService<IUserInterfaceService>();

        // Build the review-page path from the report id. The page reads the category from the report itself, so the id
        // (a bare, token-safe file name) is all that needs to be carried. See #1674.
        await userInterfaceService.OpenConfigurationWebPageAsync( $"ExceptionReport?report={settings.ReportFileName}" );

        return 0;
    }
}
