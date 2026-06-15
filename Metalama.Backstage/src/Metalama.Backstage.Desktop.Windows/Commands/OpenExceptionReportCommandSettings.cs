// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace Metalama.Backstage.Desktop.Windows.Commands;

[UsedImplicitly( ImplicitUseTargetFlags.WithMembers )]
public class OpenExceptionReportCommandSettings : BaseSettings
{
    /// <summary>
    /// Gets the id of the exception report to review: the bare file name (no directory component) of a report in the
    /// local exceptions directory (e.g. <c>exception-….xml</c>). It is token-safe (no spaces). The command builds the
    /// review-page path from it. See #1674.
    /// </summary>
    [CommandArgument( 0, "<report>" )]
    public string ReportFileName { get; init; } = null!;
}
