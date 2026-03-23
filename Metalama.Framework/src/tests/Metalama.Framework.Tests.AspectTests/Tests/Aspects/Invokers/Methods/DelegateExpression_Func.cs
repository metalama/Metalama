// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_Func;

/*
 * Tests CreateDelegateExpression targeting an overloaded method that returns a value.
 * Expected: typed delegate expression using Func<int, string> to disambiguate.
 */

public class DelegateAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override(
            nameof(this.Template),
            new
            {
                target = builder.Target.DeclaringType!.Methods.OfName( "Convert" )
                    .Single( m => m.Parameters.Count == 1 && m.Parameters[0].Type.SpecialType == SpecialType.Int32 )
            } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IMethod target )
    {
        dynamic? func = target.CreateDelegateExpression().Value;

        return meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    public string Convert( int x ) { return x.ToString(); }

    public string Convert( string s ) { return s; }

    [DelegateAspect]
    public void Invoker() { }
}
