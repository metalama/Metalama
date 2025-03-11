// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_12_0_OR_GREATER)
// @RequiredConstant(NET9_0_OR_GREATER)
#endif

#if ROSLYN_4_12_0_OR_GREATER && NET9_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.OverloadResolutionPriority;

public class TheAspect : TypeAspect
{
    [Introduce]
    [OverloadResolutionPriority(1)]
    static void M2(long x) => Console.WriteLine("New M2");

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var newM3 = builder.Target.Methods.OfExactSignature("M3", [TypeFactory.GetType(typeof(long))])!;

        builder.With(newM3).IntroduceAttribute(AttributeConstruction.Create(typeof(OverloadResolutionPriorityAttribute), [1]));
    }
}

// <target>
[TheAspect]
class Program
{
    static void M1(int x) => Console.WriteLine("Old M1");

    [OverloadResolutionPriority(1)]
    static void M1(long x) => Console.WriteLine("New M1");

    static void M2(int x) => Console.WriteLine("Old M2");

    static void M3(int x) => Console.WriteLine("Old M3");

    static void M3(long x) => Console.WriteLine("New M3");

    static void TestMain()
    {
        M1(1);
        M2(2);
        M3(3);
    }
}

#endif