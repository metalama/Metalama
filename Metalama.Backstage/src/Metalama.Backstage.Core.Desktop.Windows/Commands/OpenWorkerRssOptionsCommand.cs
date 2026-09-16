// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Backstage.Desktop.Windows.Commands;

[UsedImplicitly( ImplicitUseTargetFlags.WithMembers )]
internal class OpenWorkerRssOptionsCommand : OpenWorkerWebPageCommand
{
    public const string Name = "rss";

    public OpenWorkerRssOptionsCommand() : base( "RssOptions" ) { }
}