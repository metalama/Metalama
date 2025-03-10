// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Generic_Closed
{
    /*
     * Tests introducing closed generic type with a concrete type argument.
     */

    public interface IInterface<T>
    {
        void Foo( T t );
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder
                .ImplementInterface(
                    ( (INamedType)TypeFactory.GetType( typeof(IInterface<>) ) ).WithTypeArguments( TypeFactory.GetType( SpecialType.Int32 ) ) );

            aspectBuilder
                .ImplementInterface( ( (INamedType)TypeFactory.GetType( typeof(IInterface<>) ) ).WithTypeArguments( TypeFactory.GetType( typeof(int[]) ) ) );

            aspectBuilder
                .ImplementInterface(
                    ( (INamedType)TypeFactory.GetType( typeof(IInterface<>) ) ).WithTypeArguments( TypeFactory.GetType( typeof(Tuple<int, int>) ) ) );
        }

        [InterfaceMember]
        public void Foo( int t ) { }

        [InterfaceMember]
        public void Foo( int[] t ) { }

        [InterfaceMember]
        public void Foo( Tuple<int, int> t ) { }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}