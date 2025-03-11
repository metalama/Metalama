// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.TemplateParameterCastToIExpression;

class Aspect : ContractAspect
{
    NumericRange Range { get; } = new();

    public override void Validate(dynamic? value)
    {
        var type = ((IHasType)meta.Target.Declaration).Type;

        var expressionBuilder = new ExpressionBuilder();

        if (this.Range.GeneratePattern(type, expressionBuilder, (IExpression)value!))
        {
            if (expressionBuilder.ToValue())
            {
                this.OnContractViolated(value);
            }
        }
    }

    [Template]
    public void OnContractViolated(dynamic? value) => throw new ArgumentOutOfRangeException(nameof(value));

}

[RunTimeOrCompileTime]
class NumericRange : ICompileTimeSerializable
{
    public bool GeneratePattern(IType type, ExpressionBuilder expressionBuilder, IExpression value)
    {
        expressionBuilder.AppendExpression(value);
        expressionBuilder.AppendVerbatim(" > 0");

        return true;
    }
}

// <target>
class Target
{
    void M([Aspect] int p) { }
}