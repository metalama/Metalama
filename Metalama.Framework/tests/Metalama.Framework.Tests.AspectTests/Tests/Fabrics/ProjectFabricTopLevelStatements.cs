// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Include(Include/__TopLevelStatements.cs)
// @OutputAssemblyType(Exe)
#endif

using System;
using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.IntegrationTests.Aspects.Fabrics.ProjectFabricTopLevelStatements
{
    /*
     * Tests that applying an override on all methods does not target the Main method containing top-level statements.
     */

    public class OverrideAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "This is the overriding method." );

            return meta.Proceed();
        }
    }

    public class MyProjectFabric : ProjectFabric
    {
        public override void AmendProject( IProjectAmender amender )
        {
            amender.SelectMany( p => p.Types.SelectMany( t => t.Methods ) ).AddAspectIfEligible( m => new OverrideAttribute() );
        }
    }
}