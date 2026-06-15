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
    /// Gets the bare file name of the captured exception report to review (e.g. <c>exception-….xml</c>). It is
    /// token-safe (no spaces). The command builds the review-page path from it, so this command opens specifically an
    /// exception report rather than an arbitrary URL.
    /// </summary>
    [CommandArgument( 0, "<report>" )]
    public string Report { get; init; } = null!;
}
