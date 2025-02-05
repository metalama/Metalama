#if TEST_OPTIONS
// @IgnoredDiagnostic(CS1998)
#endif

#if NET5_0_OR_GREATER
using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Metalama.Framework;
using Metalama.Testing.AspectTesting;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncIterators.AsyncIteratorTemplate
{
    class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            throw new NotSupportedException();
        }

        public override async IAsyncEnumerable<dynamic?> OverrideAsyncEnumerableMethod()
        {
            Console.WriteLine($"Starting {meta.Target.Method.Name}");
            await foreach ( var item in meta.ProceedAsyncEnumerable() )
            {
                Console.WriteLine($" Intercepting {item}");
                yield return item;
            }
            Console.WriteLine($"Ending {meta.Target.Method.Name}");
        }

        public override async IAsyncEnumerator<dynamic?> OverrideAsyncEnumeratorMethod()
        {
            Console.WriteLine($"Starting {meta.Target.Method.Name}");
            var enumerator = meta.ProceedAsyncEnumerator();
            
            while ( await enumerator.MoveNextAsync() )
            {
                Console.WriteLine($" Intercepting {enumerator.Current}");
                yield return enumerator.Current;
            }
            Console.WriteLine($"Ending {meta.Target.Method.Name}");
        }


    }
    
    class Program
    {
        static async Task Main()
        {
            TargetCode targetCode = new();
            
            await foreach ( var a in targetCode.Enumerable(0) )
            {
                Console.WriteLine($" Received {a}");
            }
            
            
            var enumerator1 = targetCode.Enumerator(0);
            while ( await enumerator1.MoveNextAsync() )
            {
                Console.WriteLine($" Received {enumerator1.Current}");
            }
            
        }
    }

    // <target>
    class TargetCode
    {
        [Aspect]
        public async IAsyncEnumerable<int> Enumerable(int a)
        {
            await Task.Yield();
            Console.WriteLine("Yield 1");
            yield return 1;
            Console.WriteLine("Yield 2");
            yield return 2;
            Console.WriteLine("Yield 3");
            yield return 3;
        }
        
        [Aspect]
        public async IAsyncEnumerator<int> Enumerator(int a)
        {
            await Task.Yield();
            Console.WriteLine("Yield 1");
            yield return 1;
            Console.WriteLine("Yield 2");
            yield return 2;
            Console.WriteLine("Yield 3");
            yield return 3;
        }
        
      
    }
}

#endif