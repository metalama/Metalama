using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.NameConflict_CrossIntroduction;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Introduction2Attribute), typeof(Introduction1Attribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.NameConflict_CrossIntroduction
{
    /*
     * Verifies that names coming from the method builder are included in lexical scope of the property.
     */
    public class Introduction1Attribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder ) { }

        [Introduce]
        public int Foo1()
        {
            return 42;
        }

        [Introduce]
        public int Bar1()
        {
            Foo1();

            return ExpressionFactory.Parse( "Foo1()" ).Value;

            void Foo1() { }
        }
    }

    public class Introduction2Attribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder ) { }

        [Introduce]
        public int Foo2()
        {
            return 42;
        }

        [Introduce]
        public int Bar2()
        {
            Foo2();

            return ExpressionFactory.Parse( "Foo2()" ).Value;

            void Foo2() { }
        }

        [Introduce]
        public int Quz()
        {
            Foo1();

            return ExpressionFactory.Parse( "Foo1()" ).Value;

            void Foo1() { }
        }
    }

    // <target>
    [Introduction1]
    [Introduction2]
    internal class TargetClass { }
}