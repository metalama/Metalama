using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.SyntaxBuilders.SwitchWithWhen;

public class TheAspect : TypeAspect
{
    [Introduce]
    public void TheMethod( string propertyName, string otherName )
    {
        var switchBuilder = new SwitchStatementBuilder( ExpressionFactory.Capture( propertyName ) );

        foreach (var property in meta.Target.Type.Properties)
        {
            var statementBuilder = new StatementBuilder();
            statementBuilder.AppendTypeName( typeof(Console) );
            statementBuilder.AppendVerbatim( ".WriteLine(" );
            statementBuilder.AppendLiteral( property.Name );
            statementBuilder.AppendVerbatim( ");" );

            switchBuilder.AddCase(
                SwitchStatementLabel.CreateLiteral( property.Name ),
                ExpressionFactory.Capture( otherName == "xxx" ),
                statementBuilder.ToStatement().AsList() );
        }

        switchBuilder.AddDefault( StatementFactory.Parse( "return;" ).AsList(), false );
        meta.InsertStatement( switchBuilder.ToStatement() );
    }
}

// <target>
[TheAspect]
internal class C
{
    private string? A { get; set; }

    private string? B { get; set; }
}