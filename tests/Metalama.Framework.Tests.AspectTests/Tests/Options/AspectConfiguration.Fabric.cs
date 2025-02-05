// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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