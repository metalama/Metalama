// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.IntegrationTests.Aspects.Invokers.Fields.ExpressionBuilder_Value;

public class TestAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var mappingType = (INamedType)meta.Target.Method.Parameters[0].Type;

        var from = meta.Target.Method.Parameters[0];
        var to = meta.Target.Method.Parameters[1];

        foreach (var fieldOrProperty in mappingType.FieldsAndProperties)
        {
            var eb = new ExpressionBuilder();
            eb.AppendExpression( fieldOrProperty.With( to ).Value! );
            eb.AppendVerbatim( " = " );
            eb.AppendExpression( fieldOrProperty.With( (IExpression)from.Value! ) );
            meta.InsertStatement( eb.ToExpression() );
        }

        return meta.Proceed();
    }
}

// <target>
internal class TargetClass
{
    public int F;

    [Test]
    public void Map( TargetClass source, TargetClass target ) { }
}