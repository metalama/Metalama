using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.AsyncAndIterators
{
    /*
     * Tests that async methods, iterators and async iterators are correctly introduced.
     */

    public interface IInterface
    {
        Task<int> AsyncMethod();

        IEnumerable<int> Iterator();

        IAsyncEnumerable<int> AsyncIterator();
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface) );
        }

        [InterfaceMember]
        public async Task<int> AsyncMethod()
        {
            Console.WriteLine( "Introduced" );
            await Task.Yield();

            return 42;
        }

        [InterfaceMember]
        public IEnumerable<int> Iterator()
        {
            Console.WriteLine( "Introduced" );

            yield return 42;
        }

        [InterfaceMember]
        public async IAsyncEnumerable<int> AsyncIterator()
        {
            Console.WriteLine( "Introduced" );
            await Task.Yield();

            yield return 42;
        }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}