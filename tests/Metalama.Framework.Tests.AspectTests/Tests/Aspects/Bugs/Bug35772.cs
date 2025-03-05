using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.Integration.Tests.Aspects.Bugs.Bug35772;

#pragma warning disable CS0169
public class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        if (meta.Target.Method.ReturnType.IsConvertibleTo(typeof(A<>), ConversionKind.TypeDefinition))
        {
            Console.WriteLine($"{meta.Target.Method.ReturnType} is {typeof(A<>)}");
        }
        else
        {
            Console.WriteLine($"{meta.Target.Method.ReturnType} is not {typeof(A<>)}");
        }

        return meta.Proceed();
    }
}

public class A<T> where T : struct
{
}

public class B<T> : A<T> where T : struct
{
}

public class C<T>
{
}

public enum E { }

// <target>
public partial class TargetClass
{
    [TestAspect]
    public object M1()
    {
        return new object();
    }

    [TestAspect]
    public A<E> M2()
    {
        return new A<E>();
    }

    [TestAspect]
    public B<E> M3()
    {
        return new B<E>();
    }

    [TestAspect]
    public C<E> M4()
    {
        return new C<E>();
    }
}
