// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DelegateExpression_Overloaded;

/*
 * Tests CreateDelegateExpression targeting an overloaded void method.
 * Expected: typed delegate expression using Action<int> to disambiguate.
 */

public class DelegateAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override(
            nameof(this.Template),
            new
            {
                targetInt = builder.Target.DeclaringType!.Methods.OfName( "Method" )
                    .Single( m => m.Parameters.Count == 1 && m.Parameters[0].Type.SpecialType == SpecialType.Int32 ),
                targetString = builder.Target.DeclaringType!.Methods.OfName( "Method" )
                    .Single( m => m.Parameters.Count == 1 && m.Parameters[0].Type.SpecialType == SpecialType.String )
            } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IMethod targetInt, [CompileTime] IMethod targetString )
    {
        meta.InsertComment( "Delegate to Method(int)" );
        dynamic? intDelegate = targetInt.CreateDelegateExpression().Value;

        meta.InsertComment( "Delegate to Method(string)" );
        dynamic? stringDelegate = targetString.CreateDelegateExpression().Value;

        return meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    public void Method( int x ) { }

    public void Method( string s ) { }

    [DelegateAspect]
    public void Invoker() { }
}
