#if TEST_OPTIONS
// @IgnoredDiagnostic(CS1998)
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Templating.Aspects.Iterators.IteratorTemplateGetter
{
    internal class Aspect : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public override IEnumerable<dynamic?> OverrideEnumerableProperty
        {
            get
            {
                Console.WriteLine( $"Starting {meta.Target.Method.Name}" );

                foreach (var item in meta.ProceedEnumerable())
                {
                    Console.WriteLine( $" Intercepting {item}" );

                    yield return item;
                }

                Console.WriteLine( $"Ending {meta.Target.Method.Name}" );
            }
        }

        public override IEnumerator<dynamic?> OverrideEnumeratorProperty
        {
            get
            {
                Console.WriteLine( $"Starting {meta.Target.Method.Name}" );
                var enumerator = meta.ProceedEnumerator();

                while (enumerator.MoveNext())
                {
                    Console.WriteLine( $" Intercepting {enumerator.Current}" );

                    yield return enumerator.Current;
                }

                Console.WriteLine( $"Ending {meta.Target.Method.Name}" );
            }
        }
    }

    internal class Program
    {
        private static void TestMain()
        {
            TargetCode targetCode = new();

            foreach (var a in targetCode.Enumerable)
            {
                Console.WriteLine( $" Received {a}" );
            }

            foreach (var a in targetCode.OldEnumerable)
            {
                Console.WriteLine( $" Received {a}" );
            }

            var enumerator1 = targetCode.Enumerator;

            while (enumerator1.MoveNext())
            {
                Console.WriteLine( $" Received {enumerator1.Current}" );
            }

            var enumerator2 = targetCode.OldEnumerator;

            while (enumerator2.MoveNext())
            {
                Console.WriteLine( $" Received {enumerator2.Current}" );
            }
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        public IEnumerable<int> Enumerable
        {
            get
            {
                Console.WriteLine( "Yield 1" );

                yield return 1;

                Console.WriteLine( "Yield 2" );

                yield return 2;

                Console.WriteLine( "Yield 3" );

                yield return 3;
            }
        }

        [Aspect]
        public IEnumerator<int> Enumerator
        {
            get
            {
                Console.WriteLine( "Yield 1" );

                yield return 1;

                Console.WriteLine( "Yield 2" );

                yield return 2;

                Console.WriteLine( "Yield 3" );

                yield return 3;
            }
        }

        [Aspect]
        public IEnumerable OldEnumerable
        {
            get
            {
                Console.WriteLine( "Yield 1" );

                yield return 1;

                Console.WriteLine( "Yield 2" );

                yield return 2;

                Console.WriteLine( "Yield 3" );

                yield return 3;
            }
        }

        [Aspect]
        public IEnumerator OldEnumerator
        {
            get
            {
                Console.WriteLine( "Yield 1" );

                yield return 1;

                Console.WriteLine( "Yield 2" );

                yield return 2;

                Console.WriteLine( "Yield 3" );

                yield return 3;
            }
        }
    }
}