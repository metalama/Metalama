using Contract;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Aspect
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var id = builder.Project.ServiceProvider.GetService<IContract>()?.GetDocumentationCommentId( builder.Target );

            builder.Advice.IntroduceMethod( builder.Target, nameof( Bar ), args: new { s = id } );
        }

        [Template]
        public static void Bar( [CompileTime] string s )
        {
            Console.WriteLine( s );
        }
    }
}