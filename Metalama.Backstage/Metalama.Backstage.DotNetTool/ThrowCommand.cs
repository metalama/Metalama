// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Commands;

namespace Metalama.Backstage.DotNetTool;

[UsedImplicitly( ImplicitUseTargetFlags.WithMembers )]
internal class ThrowCommand : BaseCommand<BaseCommandSettings>
{
    protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
        => throw new InvalidOperationException( "This exception is intentional." );
}