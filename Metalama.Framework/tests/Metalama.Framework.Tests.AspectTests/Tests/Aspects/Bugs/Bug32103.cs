using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32103;

public class MemberCountAspect : TypeAspect
{
    // Introduces a method that returns a dictionary of method names with the number of overloads
    // of this method.
    [Introduce]
    public Dictionary<string, MethodOverloadCount> GetMethodOverloadCount()
    {
        var dictionary = meta.Target.Type.Methods
            .GroupBy( m => m.Name )
            .Select( g => new MethodOverloadCount( g.Key, g.Count() ) )
            .ToDictionary( m => m.Name, m => m );

        return dictionary;
    }
}

public class MethodOverloadCount : IExpressionBuilder
{
    public MethodOverloadCount( string name, int count )
    {
        Name = name;
        Count = count;
    }

    public string Name { get; }

    public int Count { get; }

    public IExpression ToExpression()
    {
        var builder = new ExpressionBuilder();
        builder.AppendVerbatim( "new " );
        builder.AppendTypeName( typeof(MethodOverloadCount) );
        builder.AppendVerbatim( "(" );
        builder.AppendLiteral( Name );
        builder.AppendVerbatim( ", " );
        builder.AppendLiteral( Count );
        builder.AppendVerbatim( ")" );

        return builder.ToExpression();
    }
}

// <target>
[MemberCountAspect]
public class TargetClass
{
    public void Method1() { }

    public void Method1( int a ) { }

    public void Method2() { }
}