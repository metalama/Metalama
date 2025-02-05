using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.CopyAttributes
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceBinaryOperator(
                nameof(BinaryOperatorTemplate),
                builder.Target,
                TypeFactory.GetType( typeof(int) ),
                TypeFactory.GetType( typeof(int) ),
                OperatorKind.Addition,
                args: new { c = 42 } );

            builder.IntroduceUnaryOperator(
                nameof(UnaryOperatorTemplate),
                builder.Target,
                TypeFactory.GetType( typeof(int) ),
                OperatorKind.UnaryNegation,
                args: new { c = 42 } );

            builder.IntroduceConversionOperator(
                nameof(ConversionOperatorTemplate),
                builder.Target,
                TypeFactory.GetType( typeof(int) ),
                true,
                args: new { c = 42 } );
        }

        [Template]
        [Foo( 1 )]
        [return: Foo( 2 )]
        public dynamic? BinaryOperatorTemplate( [Foo( 3 )] dynamic? x, [CompileTime] int c, [Foo( 4 )] dynamic? y )
        {
            return y + c;
        }

        [Template]
        [Foo( 1 )]
        [return: Foo( 2 )]
        public dynamic? UnaryOperatorTemplate( [CompileTime] int c, [Foo( 3 )] dynamic? x )
        {
            return c;
        }

        [Template]
        [Foo( 1 )]
        [return: Foo( 2 )]
        public dynamic? ConversionOperatorTemplate( [CompileTime] int c, [Foo( 3 )] dynamic? x )
        {
            return c;
        }
    }

    public class FooAttribute : Attribute
    {
        public FooAttribute( int z ) { }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}