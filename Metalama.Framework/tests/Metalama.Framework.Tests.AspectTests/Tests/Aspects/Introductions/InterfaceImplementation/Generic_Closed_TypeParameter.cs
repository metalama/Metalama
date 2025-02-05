using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Generic_Closed_TypeParameter
{
    /*
     * Tests introducing closed generic type with a type parameter type argument.
     */

    public interface IInterface<T>
    {
        void Foo( T t );
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            void ImplementInterface( IType typeArgument )
            {
                aspectBuilder
                    .ImplementInterface( ( (INamedType)TypeFactory.GetType( typeof(IInterface<>) ) ).WithTypeArguments( typeArgument ) );

                aspectBuilder.IntroduceMethod( nameof(Foo), args: new { T = typeArgument } );
            }

            ImplementInterface( aspectBuilder.Target.TypeParameters[0] );
            ImplementInterface( aspectBuilder.Target.TypeParameters[0].MakeArrayType() );

            ImplementInterface(
                ( (INamedType)TypeFactory.GetType( typeof(Tuple<,>) ) ).WithTypeArguments(
                    aspectBuilder.Target.TypeParameters[0],
                    aspectBuilder.Target.TypeParameters[0].MakeArrayType() ) );
        }

        [Template]
        public void Foo<[CompileTime] T>( T t ) { }
    }

    // <target>
    [Introduction]
    public class TargetClass<T> { }
}