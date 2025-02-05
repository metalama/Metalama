// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Spectre.Console.Cli;

namespace Metalama.Backstage.Desktop.Windows.Commands;

[UsedImplicitly( ImplicitUseTargetFlags.WithMembers )]
public class NotifyCommandSettings : BaseSettings
{
    [CommandArgument( 0, "<kind>" )]
    public string Kind { get; init; } = null!;

    [CommandOption( "--text" )]
    public string? Text { get; init; }

    [CommandOption( "--title" )]
    public string? Title { get; init; }

    [CommandOption( "--uri" )]
    public string? Uri { get; init; }
}