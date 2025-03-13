// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Fabrics;
using Metalama.Framework.Aspects;
using System.Linq;

internal class Fabric : ProjectFabric
{
    public override void AmendProject(IProjectAmender project)
    {
        // Selecting the compiler-generated Main method causes an assertion failure.
        project.Outbound.SelectMany( p => p.Types.SelectMany( t => t.Methods ) ).AddAspectIfEligible<LogAttribute>();

        // Workaround:
        // project.With( p => p.Types.SelectMany( t => t.Methods.Where( m => m.Name != "<Main>$" ) ) ).AddAspect<LogAttribute>();
    }
}