using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.NameConflict_BaseIntroduction
{
    /*
     * Verifies that names coming from methods introduced to the base class are included in lexical scope.
     */

    public class Introduction1Attribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder ) { }

        [Introduce]
        public int Foo()
        {
            return 42;
        }
    }

    public class Introduction2Attribute : TypeAspect
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

    [Introduction1]
    internal class BaseClass { }

    // <target>
    [Introduction2]
    internal class TargetClass : BaseClass { }
}