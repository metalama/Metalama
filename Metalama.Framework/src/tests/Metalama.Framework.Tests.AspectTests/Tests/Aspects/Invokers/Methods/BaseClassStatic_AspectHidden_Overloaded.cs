// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden_Overloaded;
using System.Linq;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(InvokerAfterAspect), typeof(IntroductionAspect), typeof(InvokerBeforeAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassStatic_AspectHidden_Overloaded;

/*
 * Tests invokers targeting overloaded static methods where only one overload is hidden by an aspect-introduced method.
 * This verifies that the resolver correctly matches by signature, not just by name.
 */

public class InvokerBeforeAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        // Target the parameterless overload (which will be hidden by the aspect introduction).
        builder.Override(
            nameof(this.Template),
            new { target = builder.Target.DeclaringType!.BaseType!.Methods.OfName( "Method" ).Single( m => m.Parameters.Count == 0 ) } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IMethod target )
    {
        meta.InsertComment( "Invoke TargetClass.Method()" );
        target.Invoke();
        meta.InsertComment( "Invoke BaseClass.Method()" );
        target.WithOptions( InvokerOptions.Base ).Invoke();
        meta.InsertComment( "Invoke TargetClass.Method()" );
        target.WithOptions( InvokerOptions.Final ).Invoke();

        return meta.Proceed();
    }
}

public class IntroductionAspect : TypeAspect
{
    // Introduces a new parameterless Method that hides BaseClass.Method().
    // The overload Method(int) is NOT hidden.
    [Introduce( WhenExists = OverrideStrategy.New )]
    public static void Method()
    {
        meta.InsertComment( "Invoke BaseClass.Method()" );
        meta.Proceed();
    }
}

public class InvokerAfterAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        // Target the parameterized overload (which is NOT hidden by the aspect introduction).
        builder.Override(
            nameof(this.Template),
            new { target = builder.Target.DeclaringType!.BaseType!.Methods.OfName( "Method" ).Single( m => m.Parameters.Count == 1 ) } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IMethod target )
    {
        meta.InsertComment( "Invoke BaseClass.Method(int) - not hidden, should stay on BaseClass" );
        target.Invoke( 42 );
        meta.InsertComment( "Invoke BaseClass.Method(int)" );
        target.WithOptions( InvokerOptions.Base ).Invoke( 42 );
        meta.InsertComment( "Invoke BaseClass.Method(int)" );
        target.WithOptions( InvokerOptions.Final ).Invoke( 42 );

        return meta.Proceed();
    }
}

public class BaseClass
{
    public static void Method() { }

    public static void Method( int x ) { }
}

// <target>
[IntroductionAspect]
public class TargetClass : BaseClass
{
    [InvokerBeforeAspect]
    public void InvokerBefore() { }

    [InvokerAfterAspect]
    public void InvokerAfter() { }
}
