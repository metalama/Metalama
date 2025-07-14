// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.ManyOverloads;

/*
 * Tests default and final invokers targeting a method declared in a different class.
 */

public class InvokerAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override( nameof(Template) );
    }

    [Template]
    public dynamic? Template()
    {
        var intMethod = meta.Target.Type.Methods.OfName( "Method" ).Single( m => m.Parameters[0].Type.SpecialType == SpecialType.Int32 );
        intMethod.Invoke( 0 );
        
        var longMethod = meta.Target.Type.Methods.OfName( "Method" ).Single( m => m.Parameters[0].Type.SpecialType == SpecialType.Int64 );
        longMethod.Invoke( 0 );
        
        var aMethod = meta.Target.Type.Methods.OfName( "Method" ).Single( m => m.Parameters[0].Type is INamedType { Name: "A"} );
        aMethod.Invoke( new B() );
        
        var bMethod = meta.Target.Type.Methods.OfName( "Method" ).Single( m => m.Parameters[0].Type is INamedType { Name: "B"} );
        bMethod.Invoke( new B() );

        return meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    [InvokerAspect]
    public void Invoker() { }

    public void Method( int i ) { }
    public void Method( long i ) { }
    public void Method( A i ) { }
    public void Method( B i ) { }
}

public class A;
public class B : A;