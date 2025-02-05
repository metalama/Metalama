using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Highlighting.IfStatements.TemplateKeywords
{
    class Aspect : IAspect
    {
        [Template]
        dynamic? Template()
        {
            meta.CompileTime( 0 );
            meta.CompileTime<long>( 0 );
            meta.InsertComment( "Hey" );
            return meta.Proceed();
        }
    }
}
