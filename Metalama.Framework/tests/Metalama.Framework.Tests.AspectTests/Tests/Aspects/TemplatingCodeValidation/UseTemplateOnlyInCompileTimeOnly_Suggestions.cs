using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.UseTemplateOnlyInCompileTimeOnly_Suggestions;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.InsertStatement( GetLoggingExpression( meta.Target.Parameters[0] ) );

        return meta.Proceed();
    }

    private IExpression GetLoggingExpression( IParameter parameter )
    {
        var builder = new ExpressionBuilder();

        builder.AppendTypeName( typeof(Console) );
        builder.AppendVerbatim( ".WriteLine(\"this: {0}, {1}: {2}\", " );
        builder.AppendExpression( ExpressionFactory.This() );
        builder.AppendVerbatim( ", " );
        builder.AppendLiteral( parameter.Name );
        builder.AppendVerbatim( ", " );
        builder.AppendExpression( parameter );
        builder.AppendVerbatim( ")" );

        return builder.ToExpression();
    }
}

// <target>
internal class Target
{
    [Aspect]
    private void M( object obj ) { }
}