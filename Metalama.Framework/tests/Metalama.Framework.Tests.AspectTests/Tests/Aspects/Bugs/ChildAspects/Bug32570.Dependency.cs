// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using System;
using System.Linq;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32570;
using Metalama.Framework.Code;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(ParameterContractAspect), typeof(OverrideAspect), typeof(IntroductionAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32570;

public class MethodFabric : TransitiveProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.SelectMany( x => x.Types.SelectMany( t => t.Methods.Where( m => m.ReturnType != TypeFactory.GetType( SpecialType.Void ) ) ) )
            .AddAspect<OverrideAspect>();

        amender
            .SelectMany(
                x => x.Types.SelectMany(
                    t => t.Methods.Where( m => m.ReturnType != TypeFactory.GetType( SpecialType.Void ) ).Select( x => x.ReturnParameter ) ) )
            .AddAspect<ParameterContractAspect>();

        amender
            .SelectMany(
                x => x.Types.SelectMany(
                    t => t.Methods.Where( m => m.ReturnType != TypeFactory.GetType( SpecialType.Void ) ).SelectMany( x => x.Parameters ) ) )
            .AddAspect<ParameterContractAspect>();
    }
}

public class TypeFabric : TransitiveProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.SelectMany( x => x.Types ).Where( t => !t.IsStatic ).AddAspect<IntroductionAspect>();
    }
}

public class IntroductionAspect : TypeAspect
{
    [Introduce]
    public int Bar( int x )
    {
        Console.WriteLine( "Introduced" );

        return x;
    }

    [Introduce]
    public void Bar_Void( int x )
    {
        Console.WriteLine( "Introduced" );
    }
}

public class OverrideAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Overridden" );

        return meta.Proceed();
    }
}

public class ParameterContractAspect : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        Console.WriteLine( $"Validate {meta.Target.Parameter.Name}" );
    }
}