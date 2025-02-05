// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug30972
{
    public class TestAspect : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get
            {
                Console.WriteLine( "Aspect" );

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "Aspect" );
                meta.Proceed();
            }
        }
    }

    public class FieldsFabric : ProjectFabric
    {
        public override void AmendProject( IProjectAmender amender )
        {
            amender.SelectMany( p => p.Types.SelectMany( t => t.Fields ) ).AddAspect<TestAspect>();
        }
    }

    // <target>
    public class TargetClass
    {
        public const int X = 42;

        public int Control;
    }

    // <target>
    public enum TargetEnum
    {
        X = 42
    }
}