#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER)
#endif

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using  Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug35474;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(IntroductionAspect), typeof(ReadAspect))]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug35474;

public class IntroductionAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.IntroduceConstructor(
            nameof(ConstructorTemplate),
            buildConstructor: c =>
            {
                c.Parameters[0].Type = builder.Target.TypeArguments[0];
            } );

        builder.IntroduceMethod(
            nameof(IntroducedMethod),
            buildMethod: m =>
            {
                m.ReturnType = builder.Target.TypeArguments[0];
                m.Parameters[0].Type = builder.Target.TypeArguments[0];
            } );
    }

    [Template]
    void ConstructorTemplate( dynamic x ) { }

    [Template]
    dynamic IntroducedMethod( dynamic arg )
    {
        return default;
    }
}

public class ReadAspect : TypeAspect
{
    [Introduce(WhenExists = OverrideStrategy.New)]
    public void PrintBaseConstructors()
    {
        foreach (var c in meta.Target.Type.BaseType.Constructors.OrderBy( c=>c.ToString() ))
        {
            Console.WriteLine( c.ToDisplayString(  ) );
        }
    }
    
    [Introduce(WhenExists = OverrideStrategy.New)]
    public void PrintBaseMethods()
    {
        foreach (var c in meta.Target.Type.BaseType.Methods.OrderBy( m=>m.ToString() ))
        {
            Console.WriteLine( c.ToDisplayString(  ) );
        }
    }
}

// <target>
[IntroductionAspect,ReadAspect]
internal class C<T>
{
    public C() { }

    public void M() { }
}

// <target>
[ReadAspect]
internal class D : C<int> { }