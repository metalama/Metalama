// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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