// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.CrossAssembly
{
    /*
     * Tests that async methods, iterators and async iterators are correctly introduced.
     */

    public interface IInterface
    {
        T Method<T>( T x );

        int Property { get; set; }

        int AutoProperty { get; set; }

        event EventHandler Event;

        event EventHandler? EventField;

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
        public T Method<T>( T x )
        {
            Console.WriteLine( "Introduced" );

            return x;
        }

        [InterfaceMember]
        public int Property
        {
            get
            {
                Console.WriteLine( "Introduced" );

                return 42;
            }

            set
            {
                Console.WriteLine( "Introduced" );
            }
        }

        [InterfaceMember]
        public int AutoProperty { get; set; }

        [InterfaceMember]
        public event EventHandler Event
        {
            add
            {
                Console.WriteLine( "Introduced" );
            }

            remove
            {
                Console.WriteLine( "Introduced" );
            }
        }

        [InterfaceMember]
        public event EventHandler? EventField;

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
}