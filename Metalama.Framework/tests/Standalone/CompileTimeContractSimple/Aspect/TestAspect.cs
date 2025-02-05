using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;

namespace Aspect
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var symbol = builder.Target.GetSymbol();

            var id = symbol.GetDocumentationCommentId();

            builder.Advice.IntroduceMethod( builder.Target, nameof(Bar), args: new { s = id } );
        }

        [Template]
        public static void Bar( [CompileTime] string s )
        {
            Console.WriteLine( s );
        }
    }
}