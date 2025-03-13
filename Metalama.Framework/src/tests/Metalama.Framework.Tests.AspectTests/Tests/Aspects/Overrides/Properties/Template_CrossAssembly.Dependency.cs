// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.Template_CrossAssembly
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var property in builder.Target.Properties)
            {
                builder.With( property ).Override( nameof(Override) );
            }
        }

        [Template]
#pragma warning disable CA1822 // Mark members as static
        public string? Override
#pragma warning restore CA1822 // Mark members as static
        {
            get
            {
                Console.WriteLine( "Aspect code" );

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "Aspect code" );
                meta.Proceed();
            }
        }
    }
}