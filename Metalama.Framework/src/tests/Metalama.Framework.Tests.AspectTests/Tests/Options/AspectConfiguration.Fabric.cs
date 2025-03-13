// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;
using System.Linq;

namespace Doc.AspectConfiguration
{
    // The project fabric configures the project at compile time.
    public class Fabric : ProjectFabric
    {
        public override void AmendProject( IProjectAmender amender )
        {
            amender
                .SetOptions( new LoggingOptions { Category = "GeneralCategory" } );

            amender
                .Select( x => x.GlobalNamespace.GetDescendant( "Doc.AspectConfiguration.Doc.ChildNamespace" )! )
                .SetOptions( new LoggingOptions() { Category = "ChildCategory" } );
        }
    }
}