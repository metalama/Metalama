using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Pragma.StatementBuilderT
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var builder = new StatementBuilder();
            builder.AppendVerbatim( "for ( int i = 0; i < n; i++ )" );
            builder.BeginBlock();
            builder.AppendVerbatim( "if ( i == 5 )" );
            builder.BeginBlock();
            builder.AppendVerbatim( "return default(" );
            builder.AppendTypeName( meta.Target.Type );
            builder.AppendVerbatim( ");" );
            builder.EndBlock();
            builder.AppendVerbatim( "Console.WriteLine(\"Hello, world.\");" );
            builder.EndBlock();

            // Emitting as a comment to check that indentation is correct.
            meta.InsertComment( "\n" + builder.ToString() );

            meta.InsertStatement( builder.ToStatement() );

            return default;
        }
    }

    internal class TargetCode
    {
        private int Method( int a )
        {
            return a;
        }
    }
}