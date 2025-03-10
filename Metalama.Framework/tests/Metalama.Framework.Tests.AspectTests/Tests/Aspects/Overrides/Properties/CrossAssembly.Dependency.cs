// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Properties.CrossAssembly;
using System;
using System.Collections.Generic;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Properties.CrossAssembly
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public int IntroducedProperty
        {
            get
            {
                Console.WriteLine( "Original" );

                return 42;
            }
            set
            {
                Console.WriteLine( "Original" );
            }
        }

        [Introduce]
        public int IntroducedProperty_Expression => 42;

        [Introduce]
        public int IntroducedProperty_Auto { get; set; }

        [Introduce]
        public int IntroducedProperty_AutoInitializer { get; set; } = 42;

        [Introduce]
        public int IntroducedProperty_InitOnly
        {
            get
            {
                Console.WriteLine( "Original" );

                return 42;
            }
            init
            {
                Console.WriteLine( "Original" );
            }
        }

        [Introduce]
        public IEnumerable<int> IntroducedProperty_Iterator
        {
            get
            {
                Console.WriteLine( "Original" );

                yield return 42;
            }
        }
    }

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var property in builder.Target.Properties)
            {
                builder.With( property ).OverrideAccessors( nameof(Template), nameof(Template) );
            }
        }

        [Template]
        public dynamic? Template()
        {
            Console.WriteLine( "Override" );

            return meta.Proceed();
        }
    }
}