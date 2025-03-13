// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Fabrics;
using System;
using System.Linq;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.SecondaryInstancesOrderingWithInheritance;

// Tests that secondary instances are sorted properly according to the declaration depth.

[assembly: MyAspect( "Assembly" )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.SecondaryInstancesOrderingWithInheritance;

[AttributeUsage( AttributeTargets.Class | AttributeTargets.Assembly | AttributeTargets.Method, AllowMultiple = false )]
[Inheritable]
public class MyAspect : OverrideMethodAspect, IAspect<ICompilation>, IAspect<INamedType>
{
    private string _tag;

    public MyAspect( string tag )
    {
        _tag = tag;
    }

    public void BuildAspect( IAspectBuilder<ICompilation> builder )
    {
        builder.Outbound.SelectMany( c => c.Types.SelectMany( t => t.Methods ) ).AddAspectIfEligible( _ => this );
    }

    public void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.Outbound.SelectMany( t => t.Methods ).AddAspect( _ => this );
    }

    public void BuildEligibility( IEligibilityBuilder<ICompilation> builder ) { }

    public void BuildEligibility( IEligibilityBuilder<INamedType> builder ) { }

    public override dynamic? OverrideMethod()
    {
        var otherInstances = meta.AspectInstance.SecondaryInstances.Select(
            x =>
                $"{( (MyAspect)x.Aspect )._tag}({x.Predecessors[0].Kind},{x.Predecessors[0].Instance.PredecessorDegree})" );

        Console.WriteLine( $"Aspect order: {_tag}, {string.Join( ", ", otherInstances )}" );

        return meta.Proceed();
    }
}

// <target>
[MyAspect( "BaseClass" )]
internal class BaseClass
{
    [MyAspect( "BaseMethod.M" )]
    public virtual void M() { }
}

// <target>
[MyAspect( "DerivedClass" )]
internal class DerivedClass : BaseClass
{
    [MyAspect( "DerivedClass.M" )]
    public override void M() { }
}