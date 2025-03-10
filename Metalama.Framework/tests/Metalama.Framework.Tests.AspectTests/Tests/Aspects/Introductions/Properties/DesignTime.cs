// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.DesignTime
{
    public class IntroductionAttribute : TypeAspect
    {
        // TODO: Indexers.    

        //[IntroduceProperty]
        //public int IntroducedProperty_Auto { get; set; }

        // TODO: Introduction of auto properties.
        //[IntroduceProperty]
        //public static int IntroducedProperty_Auto_Static { get; }

        [Introduce]
        public int IntroducedProperty_Accessors
        {
            get
            {
                Console.WriteLine( "Get" );

                return 42;
            }

            set
            {
                Console.WriteLine( value );
            }
        }
    }

    // <target>
    [Introduction]
    internal partial class TargetClass { }
}