using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.TryFindImplementationForInterfaceMember_FromBase;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(IntroduceInterfaceAttribute), typeof(CheckInterfaceAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.TryFindImplementationForInterfaceMember_FromBase;

public class IntroduceInterfaceAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
    {
        aspectBuilder.ImplementInterface( typeof(IInterface) );
    }

    [InterfaceMember( IsExplicit = true )]
    public void M1( int i ) { }

    [Introduce]
    public int M2() => 0;
}

public class CheckInterfaceAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
    {
        var methods = ( (INamedType)TypeFactory.GetType( typeof(IInterface) ) ).Methods;

        aspectBuilder.Target.TryFindImplementationForInterfaceMember( methods.OfName( "M1" ).Single(), out var m1 );
        aspectBuilder.Target.TryFindImplementationForInterfaceMember( methods.OfName( "M2" ).Single(), out var m2 );

        if (m1 == null || m2 == null)
        {
            throw new Exception( $"m1: {m1}, m2: {m2}" );
        }
    }
}

public interface IInterface
{
    void M1( int i );

    int M2();
}

// <target>
[IntroduceInterface]
internal class BaseClass { }

// <target>
[CheckInterfaceAttribute]
internal class TargetClass : BaseClass { }