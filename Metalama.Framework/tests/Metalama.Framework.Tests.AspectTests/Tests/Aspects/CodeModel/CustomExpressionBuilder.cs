using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.CodeModel.CustomExpressionBuilder
{
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            var f = meta.CompileTime( new Fixture( "Hello, world." ) );
            Console.WriteLine( f );
            var list = meta.CompileTime( new List<Fixture>() { f } );
            var runTimeList = meta.RunTime( list );

            return meta.Proceed();
        }
    }

    internal class Fixture : IExpressionBuilder
    {
        private string _msg;

        public Fixture( string msg )
        {
            _msg = msg;
        }

        public IExpression ToExpression()
        {
            ExpressionBuilder builder = new();
            builder.AppendVerbatim( "new " );
            builder.AppendTypeName( typeof(Fixture) );
            builder.AppendVerbatim( "(" );
            builder.AppendLiteral( _msg );
            builder.AppendVerbatim( ")" );

            return builder.ToExpression();
        }
    }

    internal class TargetCode
    {
        // <target>
        [Aspect]
        private int Method( int a )
        {
            return a;
        }
    }
}