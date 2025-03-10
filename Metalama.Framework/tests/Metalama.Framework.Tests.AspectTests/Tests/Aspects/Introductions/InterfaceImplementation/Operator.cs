// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
// @RequiredConstant(ROSLYN4_4_OR_GREATER)
#endif

#if NET8_0_OR_GREATER && ROSLYN4_4_OR_GREATER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Operator;

public interface I<T> where T : I<T>
{
    static abstract T operator !(T value);
}

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var ic = ((INamedType)TypeFactory.GetType(typeof(I<>))).WithTypeArguments(builder.Target);

        builder.ImplementInterface( ic);

        builder.IntroduceUnaryOperator( nameof(Foo), builder.Target, builder.Target, OperatorKind.LogicalNot);
    }

    [Template]
    public dynamic Foo(dynamic value) => value;
}

// <target>
[Introduction]
public class TargetClass { }

#endif