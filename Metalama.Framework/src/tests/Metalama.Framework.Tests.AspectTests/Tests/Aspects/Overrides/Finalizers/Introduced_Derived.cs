// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Finalizers.Introduced_Derived
{
    /*
     * Tests overriding an introduced with a base class having a finalizer works properly.
     */

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var introductionResult = builder.IntroduceFinalizer( nameof(IntroduceTemplate) );
            builder.With( introductionResult.Declaration ).Override( nameof(OverrideTemplate) );
        }

        [Template]
        public dynamic? IntroduceTemplate()
        {
            Console.WriteLine( "This is the introduction." );

            return meta.Proceed();
        }

        [Template]
        public dynamic? OverrideTemplate()
        {
            Console.WriteLine( "This is the override." );

            return meta.Proceed();
        }
    }

    // <target>
    internal class BaseClass
    {
        ~BaseClass()
        {
            Console.WriteLine( $"This is the original finalizer." );
        }
    }

    // <target>
    [Override]
    internal class DerivedClass : BaseClass { }
}