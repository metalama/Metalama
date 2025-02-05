using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Code.SyntaxBuilders;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.UseTemplateOnlyInCompileTimeOnly;

[CompileTime]
internal class C
{
    private void M( IMethodInvoker invoker )
    {
        meta.Proceed();

        meta.ProceedAsync();

        meta.InsertStatement( ExpressionFactory.Capture( 42 ) );

        invoker.With( "" );
    }

    private IExpression GetLoggingExpression( IParameter parameter )
    {
        var builder = new ExpressionBuilder();

        builder.AppendTypeName( typeof(Console) );
        builder.AppendVerbatim( ".WriteLine(\"this: {0}, {1}: {2}\", " );
        builder.AppendExpression( meta.This );
        builder.AppendVerbatim( ", " );
        builder.AppendExpression( parameter.Name );
        builder.AppendVerbatim( ", " );
        builder.AppendExpression( parameter.Value );
        builder.AppendVerbatim( ")" );

        return builder.ToExpression();
    }
}