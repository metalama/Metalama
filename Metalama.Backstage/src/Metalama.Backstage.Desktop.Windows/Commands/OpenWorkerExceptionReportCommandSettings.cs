// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace Metalama.Backstage.Desktop.Windows.Commands;

[UsedImplicitly( ImplicitUseTargetFlags.WithMembers )]
public class OpenWorkerExceptionReportCommandSettings : BaseSettings
{
    /// <summary>
    /// Gets the worker review-page relative path, including its query string (e.g.
    /// <c>ExceptionReport?report=exception-….xml&amp;category=Exception</c>). It is token-safe (no spaces).
    /// </summary>
    [CommandArgument( 0, "<page>" )]
    public string Page { get; init; } = null!;
}
