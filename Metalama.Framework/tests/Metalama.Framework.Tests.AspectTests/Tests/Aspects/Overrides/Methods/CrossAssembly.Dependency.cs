// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.CrossAssembly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.CrossAssembly
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public T IntroducedMethod_Generic<T>( T x )
        {
            Console.WriteLine( "Original" );

            return x;
        }

        [Introduce]
        public int IntroducedMethod_Expression( int x ) => x;

        [Introduce]
        public async Task<int> IntroducedMethod_TaskAsync()
        {
            Console.WriteLine( "Introduced" );
            await Task.Yield();

            return 42;
        }

        [Introduce]
        public async void IntroducedMethod_VoidAsync()
        {
            Console.WriteLine( "Introduced" );
            await Task.Yield();
        }

        [Introduce]
        public IEnumerable<int> IntroducedMethod_Iterator()
        {
            Console.WriteLine( "Introduced" );

            yield return 42;
        }
    }

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var method in builder.Target.Methods)
            {
                builder.With( method ).Override( nameof(Template) );
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