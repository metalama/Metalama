// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq.Expressions;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.NestedRunTimeOrCompileTimeInvocations;

public class AspectAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Expression.Property( Expression.Parameter( typeof(int), "p" ), "propertyName" );
        var p = Expression.Property( Expression.Parameter( typeof(int), "p" ), "propertyName" );

        return Expression.Property( Expression.Parameter( typeof(int), "p" ), "propertyName" );
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private Expression? M() => null;
}