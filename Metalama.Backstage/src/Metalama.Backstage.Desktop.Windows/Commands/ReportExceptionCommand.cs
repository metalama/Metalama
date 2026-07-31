// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;

namespace Metalama.Backstage.Desktop.Windows.Commands;

/// <summary>
/// Sends a captured exception report straight away, without opening the review page. Activated by the Report button of
/// the exception notification.
/// </summary>
/// <remarks>
/// This is the one-click alternative to Review, for the user who is willing to report but does not want to inspect the
/// payload first. The consent is still explicit (the user clicked Report), and what is sent is the same scrubbed report
/// the review page would have shown: this button changes how much the user reads, never what leaves the machine.
/// See #1751.
/// </remarks>
[UsedImplicitly( ImplicitUseTargetFlags.WithMembers )]
internal sealed class ReportExceptionCommand : BaseCommand<ExceptionReportCommandSettings>
{
    public const string Name = "report-exception";

    protected override int Execute( ExtendedCommandContext context, ExceptionReportCommandSettings settings )
    {
        var exceptionReportManager = context.ServiceProvider.GetRequiredBackstageService<IExceptionReportManager>();

        if ( !exceptionReportManager.SendReport( settings.Report ) )
        {
            // The report was removed in the meantime, or the name is invalid. There is no interface to report this to
            // (the notification is gone), so we only trace it.
            context.Logger.Warning?.Log( $"The exception report '{settings.Report}' could not be sent." );

            return 1;
        }

        context.Logger.Trace?.Log( $"The exception report '{settings.Report}' was sent." );

        return 0;
    }
}
