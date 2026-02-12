// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Finalizers.AdviceResult
{
    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var result = builder.With( builder.Target.Finalizer! ).Override( nameof(Template) );

            if (result.AdviceKind != AdviceKind.OverrideFinalizer)
            {
                throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of OverrideFinalizer." );
            }
        }

        [Template]
        public dynamic? Template()
        {
            Console.WriteLine( "This is the override." );

            return meta.Proceed();
        }
    }

    // <target>
    [Override]
    internal class TargetClass
    {
        ~TargetClass()
        {
            Console.WriteLine( $"This is the original finalizer." );
        }
    }
}
