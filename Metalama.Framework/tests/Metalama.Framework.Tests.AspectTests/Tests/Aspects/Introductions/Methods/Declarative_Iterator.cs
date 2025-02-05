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