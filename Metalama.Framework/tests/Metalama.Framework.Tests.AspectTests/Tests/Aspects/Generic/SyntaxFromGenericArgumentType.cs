using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Generic.SyntaxFromGenericArgumentType;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( meta.Target.Method.ReturnType.ToType() );

        return meta.Proceed();
    }
}

// <target>
internal class TargetCode<T>
{
    [Aspect]
    private Task<T> GenericClassTypeParameter() => null!;

    [Aspect]
    private Task<TM> GenericMethodTypeParameter<TM>() => null!;

    [Aspect]
    private Task<int> ClosedGeneric() => null!;

    [Aspect]
    private T[] ArrayClassTypeParameter() => null!;

    [Aspect]
    private TM[] ArrayMethodTypeParameter<TM>() => null!;

    [Aspect]
    private int[] ClosedArray() => null!;

    [Aspect]
    private Action<T, TM, T[], TM[], int, int[], Action<T, TM>> ComplexType<TM>() => null!;
}