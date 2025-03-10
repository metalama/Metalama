// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Commands;
using Metalama.Framework.Engine.Utilities;

namespace Metalama.Tool;

[UsedImplicitly]
internal class VersionCommand : BaseCommand<BaseCommandSettings>
{
    protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
    {
        context.Console.WriteSuccess( EngineAssemblyMetadataReader.Instance.PackageVersion ?? "<unknown>" );
    }
}