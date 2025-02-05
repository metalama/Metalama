#if TEST_OPTIONS
// @Skipped(Unbound type introductions not supported.)
#endif
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.TryFindImplementationForInterfaceMember_FromBase_Generic2;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(IntroduceInterfaceAttribute), typeof(CheckInterfaceAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.TryFindImplementationForInterfaceMember_FromBase_Generic2;

public class IntroduceInterfaceAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
    {
        var typeParameter = aspectBuilder.Target.TypeParameters[0];
        var interfaceType = ((INamedType) TypeFactory.GetType( typeof(IInterface<>) )).WithTypeArguments( typeParameter );
        aspectBuilder.ImplementInterface( interfaceType );

        aspectBuilder.IntroduceMethod( nameof(M1), args: new { T = typeParameter } );
        aspectBuilder.IntroduceMethod( nameof(M2), args: new { T = typeParameter } );
    }

    [Template]
    public void M1<[CompileTime]T>( T i ) { }

    [Template]
    public T M2<[CompileTime] T>() => default;
}

public class CheckInterfaceAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
    {
        var methods = ( (INamedType)TypeFactory.GetType( typeof(IInterface<int>) ) ).Methods;

        aspectBuilder.Target.TryFindImplementationForInterfaceMember( methods.OfName( "M1" ).Single(), out var m1 );
        aspectBuilder.Target.TryFindImplementationForInterfaceMember( methods.OfName( "M2" ).Single(), out var m2 );

        if (m1 == null || m2 == null)
        {
            throw new Exception( $"m1: {m1}, m2: {m2}" );
        }
    }
}

public interface IInterface<T>
{
    void M1( T i );

    T M2();
}

// <target>
[IntroduceInterface]
internal class BaseClass<T> { }

// <target>
[CheckInterfaceAttribute]
internal class TargetClass : BaseClass<int> { }