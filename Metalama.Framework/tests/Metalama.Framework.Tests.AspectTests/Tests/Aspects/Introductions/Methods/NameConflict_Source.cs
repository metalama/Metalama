using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.NameConflict_Source
{
    /*
     * Verifies that names coming from the method builder are included in lexical scope of the property.
     */
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder ) { }

        [Introduce]
        public int Bar()
        {
            Foo();

            return ExpressionFactory.Parse( "Foo()" ).Value;

            void Foo() { }
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public int Foo()
        {
            return 42;
        }
    }
}