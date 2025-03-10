// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.Declarative_Iterator
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public IEnumerable<int> IntroducedMethod_Enumerable()
        {
            Console.WriteLine( "This is introduced method." );

            yield return 42;

            foreach (var x in meta.Proceed()!)
            {
                yield return x;
            }
        }

        [Introduce]
        public IEnumerator<int> IntroducedMethod_Enumerator()
        {
            Console.WriteLine( "This is introduced method." );

            yield return 42;

            var enumerator = meta.Proceed()!;

            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}