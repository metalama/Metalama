// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.CastExpression;

public class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var sbyteValue = ExpressionFactory.Literal( 5 ).CastTo<sbyte>().Value;
        var intValue = ExpressionFactory.Literal( 5 ).CastTo<int>().Value;

        return meta.Proceed();
    }
}

// <target>
internal class C
{
    [Aspect]
    private int M() => 0;
}