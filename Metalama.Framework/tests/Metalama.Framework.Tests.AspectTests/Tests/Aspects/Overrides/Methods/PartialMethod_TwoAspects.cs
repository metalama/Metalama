// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.PartialMethod_TwoAspects;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Override2Attribute), typeof(Override1Attribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.PartialMethod_TwoAspects
{
    /*
     * Tests that overriding partial methods does work when there are multiple overrides.
     */

    public class Override1Attribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var method in builder.Target.Methods)
            {
                builder.With( method ).Override( nameof(Template), tags: new { name = method.Name } );
            }
        }

        [Template]
        public dynamic? Template()
        {
            Console.WriteLine( $"This is the override 1 of {meta.Tags["name"]}." );

            return meta.Proceed();
        }
    }

    public class Override2Attribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var method in builder.Target.Methods)
            {
                builder.With( method ).Override( nameof(Template), tags: new { name = method.Name } );
            }
        }

        [Template]
        public dynamic? Template()
        {
            Console.WriteLine( $"This is the override 2 of {meta.Tags["name"]}." );

            return meta.Proceed();
        }
    }

    // <target>
    [Override1]
    [Override2]
    internal partial class TargetClass
    {
        public partial int TargetMethod();

        partial void TargetVoidMethodNoImplementation();

        partial void TargetVoidMethodWithImplementation();
    }

    // <target>
    internal partial class TargetClass
    {
        public partial int TargetMethod()
        {
            Console.WriteLine( "This is a partial method." );

            return 42;
        }

        partial void TargetVoidMethodWithImplementation()
        {
            Console.WriteLine( "This is a partial method." );
        }
    }
}