using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Generic_Closed_TargetType
{
    /*
     * Tests introducing closed generic interface with a type argument set to the target type.
     */

    public interface IInterface<T>
    {
        void Foo( T t );
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.ImplementInterface( ( (INamedType)TypeFactory.GetType( typeof(IInterface<>) ) ).WithTypeArguments( builder.Target ) );

            builder.IntroduceMethod( nameof(Foo), args: new { T = builder.Target } );
        }

        [Template]
        public void Foo<[CompileTime] T>( T t ) { }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}